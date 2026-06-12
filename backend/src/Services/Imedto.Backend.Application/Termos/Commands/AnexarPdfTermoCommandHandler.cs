using System.Security.Cryptography;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos.Commands;

/// <summary>
/// Anexa o documento assinado fisicamente a um termo emitido pendente.
///
/// Suporta:
/// <list type="bullet">
///   <item>1 PDF (<c>application/pdf</c>): validado via magic bytes <c>%PDF-</c>.</item>
///   <item>1 ou 2 imagens (<c>image/jpeg</c> ou <c>image/png</c>): convertidas em um
///     PDF multi-página pelo handler (uma página por imagem). O PDF resultante é o
///     que vai para o S3 e tem o hash calculado.</item>
/// </list>
///
/// HEIC (<c>image/heic</c>, <c>image/heif</c>) é explicitamente rejeitado com 422
/// orientando o usuário a converter para JPG/PNG.
///
/// Validações: magic bytes reais por tipo, tamanho total ≤ 10 MB, 1-2 arquivos para
/// imagens, exatamente 1 para PDF. Multi-tenant: verifica o estabelecimento no lookup
/// do aggregate. LGPD: sem PII em logs; hash SHA-256 do PDF no audit.
/// </summary>
public sealed class AnexarPdfTermoCommandHandler : ICommandHandler<AnexarPdfTermoCommand>
{
    private readonly ITermoEmitidoRepository _repo;
    private readonly ITermoPdfStorageService _storage;
    private readonly ITermoImagemParaPdfConverter _imagemConverter;
    private readonly ITermoAuditLogger _audit;
    private readonly IEventBus _eventBus;

    /// <summary>10 MB — limite "soft" separado dos anexos clínicos (50 MB).</summary>
    public const long TamanhoMaxTotalBytes = 10L * 1024 * 1024;

    private static readonly byte[] MagicPdf  = "%PDF-"u8.ToArray();
    private static readonly byte[] MagicJpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] MagicPng  = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly HashSet<string> MimesImagem = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/jpg", "image/png" };

    private static readonly HashSet<string> MimesHeic = new(StringComparer.OrdinalIgnoreCase)
        { "image/heic", "image/heif" };

    public AnexarPdfTermoCommandHandler(
        ITermoEmitidoRepository repo,
        ITermoPdfStorageService storage,
        ITermoImagemParaPdfConverter imagemConverter,
        ITermoAuditLogger audit,
        IEventBus eventBus)
    {
        _repo = repo;
        _storage = storage;
        _imagemConverter = imagemConverter;
        _audit = audit;
        _eventBus = eventBus;
    }

    public async Task Handle(AnexarPdfTermoCommand cmd)
    {
        // 1. Multi-tenant: busca o termo vinculado ao estabelecimento.
        var termo = await _repo.ObterPorIdOuNulo(cmd.TermoEmitidoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Termo não encontrado.");

        // 2. Validações básicas da requisição.
        var partes = cmd.Partes;
        if (partes is null || partes.Count == 0)
            throw new BusinessException("Arquivo é obrigatório.");
        if (cmd.TamanhoTotalBytes <= 0 || cmd.TamanhoTotalBytes > TamanhoMaxTotalBytes)
            throw new BusinessException($"Tamanho total do arquivo excede {TamanhoMaxTotalBytes / (1024 * 1024)} MB.");
        if (partes.Count > 2)
            throw new BusinessException("Máximo de 2 arquivos por vez (frente e verso).");

        // 3. Buffer das partes em memória (≤10 MB — seguro).
        var buffers = new List<(byte[] Dados, string MimeType)>(partes.Count);
        long totalLido = 0;
        foreach (var (stream, mime, tamanho) in partes)
        {
            using var ms = new MemoryStream((int)Math.Min(tamanho, TamanhoMaxTotalBytes + 1));
            await stream.CopyToAsync(ms);
            totalLido += ms.Length;
            if (totalLido > TamanhoMaxTotalBytes)
                throw new BusinessException($"Tamanho total excede {TamanhoMaxTotalBytes / (1024 * 1024)} MB.");
            buffers.Add((ms.ToArray(), mime?.Trim() ?? ""));
        }

        // 4. Determina o modo (PDF ou imagem) e valida coerência.
        var primeiroMime = buffers[0].MimeType;

        // HEIC: rejeição explícita com orientação.
        if (MimesHeic.Contains(primeiroMime) || buffers.Any(b => MimesHeic.Contains(b.MimeType)))
            throw new BusinessException(
                "Formato HEIC não é suportado. Converta a foto para JPG ou PNG antes de enviar.");

        bool ePdf    = string.Equals(primeiroMime, "application/pdf", StringComparison.OrdinalIgnoreCase);
        bool eImagem = MimesImagem.Contains(primeiroMime);

        if (!ePdf && !eImagem)
            throw new BusinessException("Formato não suportado. Envie um PDF, JPG ou PNG.");
        if (ePdf && partes.Count > 1)
            throw new BusinessException("Para PDF, envie apenas um arquivo.");
        if (eImagem && buffers.Any(b => !MimesImagem.Contains(b.MimeType)))
            throw new BusinessException("Todos os arquivos devem ser do mesmo tipo de imagem.");

        // 5. Valida magic bytes reais (defesa contra MIME falsificado no request).
        foreach (var (dados, mime) in buffers)
        {
            if (dados.Length == 0)
                throw new BusinessException("Arquivo vazio.");

            bool magicOk = ePdf
                ? dados.Length >= MagicPdf.Length && dados.AsSpan(0, MagicPdf.Length).SequenceEqual(MagicPdf)
                : ValidarMagicImagem(dados, mime);

            if (!magicOk)
                throw new BusinessException(ePdf
                    ? "Arquivo não é um PDF válido."
                    : "Arquivo de imagem inválido ou corrompido.");
        }

        // 6. Produz o PDF final (conversão de imagem ou passagem direta).
        byte[] pdfFinal;
        if (ePdf)
        {
            pdfFinal = buffers[0].Dados;
        }
        else
        {
            // Converte 1 ou 2 imagens em um PDF multi-página.
            var streams = buffers.Select(b => (new MemoryStream(b.Dados) as Stream, b.MimeType)).ToList();
            pdfFinal = _imagemConverter.ConverterParaPdf(streams);
        }

        // 7. Hash SHA-256 do PDF resultante.
        var pdfHash = ConverterParaHex(SHA256.HashData(pdfFinal));

        // 8. Upload no S3 — fora da transação do DbContext.
        var path = $"termos/est_{cmd.EstabelecimentoId}/{cmd.TermoEmitidoId}_{Guid.NewGuid():N}.pdf";
        await _storage.UploadAsync(path, new MemoryStream(pdfFinal), "application/pdf");

        // 9. Atualiza o aggregate.
        termo.AnexarPdf(path, pdfHash);
        await _repo.Salvar(termo);

        cmd.StoragePath = path;
        cmd.PdfHash = pdfHash;

        // 10. Eventos de domínio + audit best-effort.
        foreach (var ev in termo.DomainEvents)
            await _eventBus.Publish(ev);
        termo.ClearDomainEvents();

        await _audit.RegistrarAsync(
            cmd.EstabelecimentoId, cmd.SolicitanteUsuarioId,
            "termo-pdf-anexado", "TermoEmitido", termo.Id,
            metadataJson: $"{{\"pdf_hash\":\"{pdfHash}\",\"partes\":{partes.Count}}}");
    }

    private static bool ValidarMagicImagem(byte[] dados, string mime)
    {
        if (string.Equals(mime, "image/png", StringComparison.OrdinalIgnoreCase))
            return dados.Length >= MagicPng.Length && dados.AsSpan(0, MagicPng.Length).SequenceEqual(MagicPng);

        // image/jpeg ou image/jpg
        return dados.Length >= MagicJpeg.Length && dados.AsSpan(0, MagicJpeg.Length).SequenceEqual(MagicJpeg);
    }

    private static string ConverterParaHex(byte[] bytes)
    {
        var c = new char[bytes.Length * 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            c[i * 2]     = HexChar(b >> 4);
            c[i * 2 + 1] = HexChar(b & 0xF);
        }
        return new string(c);
    }

    private static char HexChar(int v) => (char)(v < 10 ? '0' + v : 'a' + v - 10);
}
