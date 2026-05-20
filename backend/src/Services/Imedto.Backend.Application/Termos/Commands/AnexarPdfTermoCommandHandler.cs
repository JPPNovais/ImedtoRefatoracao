using System.Security.Cryptography;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos.Commands;

/// <summary>
/// Anexa um PDF assinado fisicamente a um termo emitido pendente. Valida magic bytes,
/// MIME, tamanho, faz upload no S3 e marca o termo como assinado.
/// </summary>
public sealed class AnexarPdfTermoCommandHandler : ICommandHandler<AnexarPdfTermoCommand>
{
    private readonly ITermoEmitidoRepository _repo;
    private readonly ITermoPdfStorageService _storage;
    private readonly ITermoAuditLogger _audit;
    private readonly IEventBus _eventBus;

    /// <summary>Limite "soft" — separado dos anexos clínicos (anexos podem ter 50 MB; PDFs de termo, 10).</summary>
    public const long TamanhoMaxBytes = 10L * 1024 * 1024;
    public const string MimeTypeAceito = "application/pdf";
    private static readonly byte[] MagicPdf = "%PDF-"u8.ToArray();

    public AnexarPdfTermoCommandHandler(
        ITermoEmitidoRepository repo,
        ITermoPdfStorageService storage,
        ITermoAuditLogger audit,
        IEventBus eventBus)
    {
        _repo = repo;
        _storage = storage;
        _audit = audit;
        _eventBus = eventBus;
    }

    public async Task Handle(AnexarPdfTermoCommand cmd)
    {
        var termo = await _repo.ObterPorIdOuNulo(cmd.TermoEmitidoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Termo não encontrado.");

        if (cmd.Conteudo is null)
            throw new BusinessException("Arquivo é obrigatório.");
        if (cmd.TamanhoBytes <= 0 || cmd.TamanhoBytes > TamanhoMaxBytes)
            throw new BusinessException($"Tamanho do PDF inválido (máx. {TamanhoMaxBytes / (1024 * 1024)} MB).");
        if (!string.Equals(cmd.MimeType?.Trim(), MimeTypeAceito, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("Apenas arquivos PDF são aceitos.");

        // Buffer em memória pra validar magic bytes + calcular hash + subir no S3 sem reler o stream.
        // PDFs de termo são pequenos (≤10MB) — RAM aceitável.
        using var ms = new MemoryStream();
        await cmd.Conteudo.CopyToAsync(ms);
        if (ms.Length == 0)
            throw new BusinessException("Arquivo vazio.");
        if (ms.Length > TamanhoMaxBytes)
            throw new BusinessException($"Tamanho do PDF excede {TamanhoMaxBytes / (1024 * 1024)} MB.");
        if (ms.Length < MagicPdf.Length || !ms.GetBuffer().AsSpan(0, MagicPdf.Length).SequenceEqual(MagicPdf))
            throw new BusinessException("Arquivo não é um PDF válido.");

        // Hash de integridade do PDF (auditoria).
        ms.Position = 0;
        var pdfHash = ConverterParaHex(SHA256.HashData(ms.ToArray()));

        // Upload no S3 — fora da transação do DbContext.
        var path = $"termos/est_{cmd.EstabelecimentoId}/{cmd.TermoEmitidoId}_{Guid.NewGuid():N}.pdf";
        ms.Position = 0;
        await _storage.UploadAsync(path, ms, MimeTypeAceito);

        // Atualiza o aggregate.
        termo.AnexarPdf(path, pdfHash);
        await _repo.Salvar(termo);

        cmd.StoragePath = path;
        cmd.PdfHash = pdfHash;

        foreach (var ev in termo.DomainEvents)
            await _eventBus.Publish(ev);
        termo.ClearDomainEvents();

        await _audit.RegistrarAsync(
            cmd.EstabelecimentoId, cmd.SolicitanteUsuarioId,
            "termo-pdf-anexado", "TermoEmitido", termo.Id,
            metadataJson: $"{{\"pdf_hash\":\"{pdfHash}\"}}");
    }

    private static string ConverterParaHex(byte[] bytes)
    {
        var c = new char[bytes.Length * 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            c[i * 2] = HexChar(b >> 4);
            c[i * 2 + 1] = HexChar(b & 0xF);
        }
        return new string(c);
    }

    private static char HexChar(int v) => (char)(v < 10 ? '0' + v : 'a' + v - 10);
}
