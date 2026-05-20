using System.Security.Cryptography;
using System.Text;
using Imedto.Backend.Domain.Termos.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Aggregate root — instância de termo emitido para um paciente. Carrega um snapshot
/// imutável do conteúdo no momento da emissão (importante para LGPD/audit: o que o
/// paciente assinou em 2026 não pode ser reescrito quando o modelo mudar em 2027).
///
/// Estados (<see cref="StatusTermoEmitido"/>):
/// <list type="bullet">
///   <item><c>Pendente</c>: emitido, aguarda assinatura (link público ou upload de PDF).</item>
///   <item><c>Assinado</c>: paciente aceitou (link) ou emissor anexou PDF assinado.</item>
///   <item><c>Recusado</c>: paciente recusou pelo link.</item>
///   <item><c>Revogado</c>: depois de assinado, foi cancelado pelo estabelecimento.</item>
///   <item><c>Expirado</c>: link público venceu sem aceite.</item>
/// </list>
/// </summary>
public class TermoEmitido : Entity
{
    public virtual long PacienteId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long TermoModeloId { get; protected set; }
    public virtual int VersaoModelo { get; protected set; }
    public virtual string ConteudoSnapshotHtml { get; protected set; }
    public virtual string ConteudoSnapshotTexto { get; protected set; }
    public virtual StatusTermoEmitido Status { get; protected set; }
    public virtual AssinaturaTipo AssinaturaTipo { get; protected set; }

    public virtual DateTime? AssinadoEm { get; protected set; }
    public virtual string IpAssinatura { get; protected set; }
    public virtual string UserAgentAssinatura { get; protected set; }

    /// <summary>SHA-256 hex (64 chars) do conteúdo HTML normalizado.</summary>
    public virtual string HashIntegridade { get; protected set; }

    public virtual string PdfUrl { get; protected set; }
    /// <summary>SHA-256 hex do PDF anexado, quando aplicável.</summary>
    public virtual string PdfHash { get; protected set; }

    public virtual string TokenAceite { get; protected set; }
    public virtual DateTime? TokenExpiraEm { get; protected set; }

    public virtual DateTime? RevogadoEm { get; protected set; }
    public virtual Guid? RevogadoPorUsuarioId { get; protected set; }
    public virtual string RevogadoMotivo { get; protected set; }

    public virtual Guid EmitidoPorUsuarioId { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    public const int MotivoRevogacaoMaximo = 500;

    protected TermoEmitido() { }

    /// <summary>
    /// Emite um novo termo para um paciente. O <paramref name="conteudoResolvidoHtml"/> já
    /// deve estar com as variáveis ({{paciente.nome}} etc.) substituídas e sanitizado;
    /// o aggregate apenas calcula o hash e fixa o snapshot.
    ///
    /// Para <see cref="AssinaturaTipo.AceiteLink"/>, o token gerado é retornado em
    /// <see cref="TokenAceite"/> (32 bytes urlsafe). Vence em
    /// <paramref name="ttlLinkPublico"/> a partir de agora.
    /// </summary>
    public static TermoEmitido Emitir(
        long pacienteId,
        long estabelecimentoId,
        long termoModeloId,
        int versaoModelo,
        string conteudoResolvidoHtml,
        string conteudoResolvidoTexto,
        AssinaturaTipo assinaturaTipo,
        Guid emitidoPorUsuarioId,
        TimeSpan ttlLinkPublico)
    {
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (termoModeloId <= 0)
            throw new BusinessException("Modelo é obrigatório.");
        if (versaoModelo <= 0)
            throw new BusinessException("Versão do modelo é obrigatória.");
        if (string.IsNullOrWhiteSpace(conteudoResolvidoHtml))
            throw new BusinessException("Conteúdo do termo é obrigatório.");
        if (string.IsNullOrWhiteSpace(conteudoResolvidoTexto))
            throw new BusinessException("Conteúdo (texto) é obrigatório.");
        if (emitidoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Emissor é obrigatório.");

        var snapshotHtml = NormalizarHtml(conteudoResolvidoHtml);
        var hash = CalcularHashSha256(snapshotHtml);

        string token = null;
        DateTime? expira = null;
        if (assinaturaTipo == AssinaturaTipo.AceiteLink)
        {
            if (ttlLinkPublico <= TimeSpan.Zero)
                throw new BusinessException("Validade do link deve ser positiva.");
            token = GerarTokenUrlSafe(32);
            expira = DateTime.UtcNow.Add(ttlLinkPublico);
        }

        return new TermoEmitido
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            TermoModeloId = termoModeloId,
            VersaoModelo = versaoModelo,
            ConteudoSnapshotHtml = snapshotHtml,
            ConteudoSnapshotTexto = conteudoResolvidoTexto.Trim(),
            Status = StatusTermoEmitido.Pendente,
            AssinaturaTipo = assinaturaTipo,
            HashIntegridade = hash,
            TokenAceite = token,
            TokenExpiraEm = expira,
            EmitidoPorUsuarioId = emitidoPorUsuarioId,
            CriadoEm = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Marca como emitido — anexa <see cref="TermoEmitidoEvent"/>. Chamar depois de
    /// persistir (Id já resolvido). <paramref name="canalEnvio"/> só faz sentido quando
    /// <see cref="AssinaturaTipo"/> = <see cref="AssinaturaTipo.AceiteLink"/>:
    /// "email" dispara envio automático no handler; "copia" suprime o e-mail.
    /// </summary>
    public virtual void MarcarComoEmitido(string canalEnvio = "email")
    {
        if (Id == 0)
            throw new InvalidOperationException("Termo ainda não foi persistido — Id é 0.");
        AddDomainEvent(new TermoEmitidoEvent(Id, PacienteId, EstabelecimentoId, TermoModeloId, EmitidoPorUsuarioId, AssinaturaTipo, canalEnvio ?? "email"));
    }

    /// <summary>
    /// Anexa um PDF assinado fisicamente. Só permitido para
    /// <see cref="AssinaturaTipo.PdfAnexado"/> e quando ainda pendente.
    /// </summary>
    public virtual void AnexarPdf(string pdfUrl, string pdfHash)
    {
        if (AssinaturaTipo != AssinaturaTipo.PdfAnexado)
            throw new BusinessException("Este termo não aceita anexo de PDF — escolha modalidade PDF na emissão.");
        if (Status != StatusTermoEmitido.Pendente)
            throw new BusinessException("Só é possível anexar PDF em termos pendentes.");
        if (!string.IsNullOrEmpty(PdfUrl))
            throw new BusinessException("Este termo já possui PDF anexado.");
        if (string.IsNullOrWhiteSpace(pdfUrl))
            throw new BusinessException("URL do PDF é obrigatória.");
        if (string.IsNullOrWhiteSpace(pdfHash) || pdfHash.Length != 64)
            throw new BusinessException("Hash do PDF inválido.");

        PdfUrl = pdfUrl;
        PdfHash = pdfHash;
        Status = StatusTermoEmitido.Assinado;
        AssinadoEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;

        AddDomainEvent(new TermoAssinadoEvent(Id, PacienteId, EstabelecimentoId, AssinaturaTipo, AssinadoEm.Value));
    }

    /// <summary>
    /// Registra aceite via link público (Fase 4). Só para
    /// <see cref="AssinaturaTipo.AceiteLink"/> com token ainda válido.
    /// </summary>
    public virtual void RegistrarAceitePublico(string ipOrigem, string userAgent)
    {
        if (AssinaturaTipo != AssinaturaTipo.AceiteLink)
            throw new BusinessException("Este termo não aceita assinatura por link.");
        if (Status != StatusTermoEmitido.Pendente)
            throw new BusinessException("Termo não está pendente.");
        if (TokenExpiraEm is null || TokenExpiraEm < DateTime.UtcNow)
            throw new BusinessException("Link expirado.");

        Status = StatusTermoEmitido.Assinado;
        AssinadoEm = DateTime.UtcNow;
        IpAssinatura = string.IsNullOrWhiteSpace(ipOrigem) ? null : ipOrigem.Trim();
        UserAgentAssinatura = TruncarUserAgent(userAgent);
        AtualizadoEm = DateTime.UtcNow;

        AddDomainEvent(new TermoAssinadoEvent(Id, PacienteId, EstabelecimentoId, AssinaturaTipo, AssinadoEm.Value));
    }

    public virtual void RegistrarRecusaPublica(string ipOrigem, string userAgent)
    {
        if (AssinaturaTipo != AssinaturaTipo.AceiteLink)
            throw new BusinessException("Este termo não aceita recusa por link.");
        if (Status != StatusTermoEmitido.Pendente)
            throw new BusinessException("Termo não está pendente.");
        if (TokenExpiraEm is null || TokenExpiraEm < DateTime.UtcNow)
            throw new BusinessException("Link expirado.");

        Status = StatusTermoEmitido.Recusado;
        // Reuso de assinado_em para registrar o "momento da resposta" (aceito ou recusado).
        // Mantemos o nome da coluna por compat com a Fase 1 — semanticamente, é "respondido_em".
        AssinadoEm = DateTime.UtcNow;
        IpAssinatura = string.IsNullOrWhiteSpace(ipOrigem) ? null : ipOrigem.Trim();
        UserAgentAssinatura = TruncarUserAgent(userAgent);
        AtualizadoEm = DateTime.UtcNow;

        AddDomainEvent(new TermoRecusadoEvent(Id, PacienteId, EstabelecimentoId, AssinadoEm.Value));
    }

    /// <summary>
    /// Marca o instante do último envio de e-mail do link público (cooldown anti-spam).
    /// Reutiliza <see cref="AtualizadoEm"/> como timestamp do último envio — o fluxo de
    /// reenviar-link só toca esse campo (não muda status nem snapshot).
    /// </summary>
    public virtual void MarcarReenvioLinkEmail()
    {
        if (AssinaturaTipo != AssinaturaTipo.AceiteLink)
            throw new BusinessException("Este termo não usa link de aceite.");
        if (Status != StatusTermoEmitido.Pendente)
            throw new BusinessException("Termo não está pendente.");
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Expirar()
    {
        if (AssinaturaTipo != AssinaturaTipo.AceiteLink) return;
        if (Status != StatusTermoEmitido.Pendente) return;
        if (TokenExpiraEm is null || TokenExpiraEm >= DateTime.UtcNow) return;

        Status = StatusTermoEmitido.Expirado;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Revogar(Guid usuarioId, string motivo)
    {
        if (Status != StatusTermoEmitido.Assinado)
            throw new BusinessException("Apenas termos assinados podem ser revogados.");
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário responsável é obrigatório.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessException("Motivo da revogação é obrigatório.");
        if (motivo.Trim().Length > MotivoRevogacaoMaximo)
            throw new BusinessException($"Motivo excede {MotivoRevogacaoMaximo} caracteres.");

        Status = StatusTermoEmitido.Revogado;
        RevogadoEm = DateTime.UtcNow;
        RevogadoPorUsuarioId = usuarioId;
        RevogadoMotivo = motivo.Trim();
        AtualizadoEm = DateTime.UtcNow;

        AddDomainEvent(new TermoRevogadoEvent(Id, PacienteId, EstabelecimentoId, usuarioId));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Normaliza HTML para hash: CRLF→LF, trim. Mesma regra usada no Atualizar do
    /// <see cref="TermoModelo"/> — garante que paciente e estabelecimento veem o
    /// mesmo hash mesmo se editores diferentes serializarem com EOL distintos.
    /// </summary>
    public static string NormalizarHtml(string html) =>
        html?.Replace("\r\n", "\n").Trim() ?? string.Empty;

    public static string CalcularHashSha256(string conteudo)
    {
        var bytes = Encoding.UTF8.GetBytes(conteudo ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    /// <summary>
    /// Token URL-safe (RFC 4648 §5 sem padding) de <paramref name="bytes"/> bytes de entropia.
    /// 32 bytes → 43 chars. Único e impossível de adivinhar.
    /// </summary>
    private static string GerarTokenUrlSafe(int bytes)
    {
        var buffer = RandomNumberGenerator.GetBytes(bytes);
        return Convert.ToBase64String(buffer)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string TruncarUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return null;
        var t = userAgent.Trim();
        return t.Length > 500 ? t[..500] : t;
    }
}
