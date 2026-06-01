namespace Imedto.Backend.Domain.AssinaturaDigital;

/// <summary>
/// Registro append-only de auditoria para operações de assinatura digital.
/// Não é aggregate root — é criado e lido diretamente pelo repositório/handler.
///
/// LGPD: não contém PII do paciente. Apenas IDs técnicos e metadados da operação.
/// Retenção mínima: 730 dias (implicação legal de documento médico assinado).
/// </summary>
public class AssinaturaAuditLog
{
    public virtual long Id { get; protected set; }
    public virtual long ReceitaId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    /// <summary>Guid do usuário que disparou a operação (médico ou sistema para job de expiração).</summary>
    public virtual Guid UsuarioId { get; protected set; }
    /// <summary>Ação registrada. Valores: DISPARO_ASSINATURA, DOWNLOAD_PDF_ASSINADO, WEBHOOK_CALLBACK, EXPIRAR_PENDENTE.</summary>
    public virtual string Acao { get; protected set; }
    public virtual string? StatusAnterior { get; protected set; }
    public virtual string? StatusNovo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }

    protected AssinaturaAuditLog() { }

    public static AssinaturaAuditLog Registrar(
        long receitaId,
        long estabelecimentoId,
        Guid usuarioId,
        string acao,
        string? statusAnterior,
        string? statusNovo)
    {
        return new AssinaturaAuditLog
        {
            ReceitaId = receitaId,
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = usuarioId,
            Acao = acao,
            StatusAnterior = statusAnterior,
            StatusNovo = statusNovo,
            CriadoEm = DateTime.UtcNow
        };
    }
}
