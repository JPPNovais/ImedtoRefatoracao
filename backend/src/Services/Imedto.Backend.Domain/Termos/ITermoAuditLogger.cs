namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Append-only audit logger para ações em termos. A implementação grava em
/// <c>termo_audit_log</c>. Nunca lança — log de auditoria não pode quebrar o fluxo
/// de negócio (handler captura exceções internamente).
/// </summary>
public interface ITermoAuditLogger
{
    Task RegistrarAsync(
        long? estabelecimentoId,
        Guid? usuarioId,
        string acao,
        string entidade,
        long entidadeId,
        string metadataJson = null,
        string ipOrigem = null,
        CancellationToken ct = default);
}
