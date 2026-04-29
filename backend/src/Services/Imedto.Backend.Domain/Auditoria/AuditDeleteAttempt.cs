using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Auditoria;

/// <summary>
/// Registro imutável de tentativa de exclusão física de um aggregate
/// <see cref="ISoftDeletable"/>. Populado pelo <c>SoftDeleteInterceptor</c>
/// antes de a transação ser abortada com <see cref="BusinessException"/>.
///
/// Apoia LGPD (rastreabilidade de quem tentou apagar dados sensíveis) e integridade
/// contábil (movimentações financeiras nunca podem ser fisicamente removidas).
/// </summary>
public class AuditDeleteAttempt : Entity
{
    public virtual string Tabela { get; protected set; } = string.Empty;
    public virtual string RegistroId { get; protected set; } = string.Empty;
    public virtual long? EstabelecimentoId { get; protected set; }
    public virtual Guid? UsuarioId { get; protected set; }
    public virtual string? Motivo { get; protected set; }
    public virtual DateTime TentadoEm { get; protected set; }

    protected AuditDeleteAttempt() { }

    public static AuditDeleteAttempt Registrar(
        string tabela,
        string registroId,
        long? estabelecimentoId,
        Guid? usuarioId,
        string? motivo)
    {
        if (string.IsNullOrWhiteSpace(tabela))
            throw new BusinessException("Tabela é obrigatória.");
        if (string.IsNullOrWhiteSpace(registroId))
            throw new BusinessException("Identificador do registro é obrigatório.");

        return new AuditDeleteAttempt
        {
            Tabela = tabela.Trim(),
            RegistroId = registroId.Trim(),
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = usuarioId,
            Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim(),
            TentadoEm = DateTime.UtcNow
        };
    }
}
