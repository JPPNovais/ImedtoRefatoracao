namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Serviço de reset administrativo de estabelecimento. Usado exclusivamente para
/// tooling operacional (reset de demos, limpeza de clientes cancelados).
///
/// Faz bypass do <c>SoftDeleteInterceptor</c> via SQL direto — não passa pelo EF Core.
/// Cada operação é auditada em <c>audit_delete_attempts</c> com motivo prefixado por "ADMIN_RESET:".
/// </summary>
public interface IAdminResetService
{
    /// <summary>
    /// Remove todo o conteúdo do estabelecimento (dados clínicos, financeiros, operacionais)
    /// mantendo a casca (<c>estabelecimentos</c> não é deletado). Tudo em uma única transação.
    /// </summary>
    /// <param name="estabelecimentoId">Id do estabelecimento a resetar.</param>
    /// <param name="motivo">Motivo do reset (obrigatório para auditoria).</param>
    /// <param name="executadoPorUsuarioId">Id do admin que executou.</param>
    Task ResetEstabelecimentoAsync(
        long estabelecimentoId,
        string motivo,
        Guid executadoPorUsuarioId,
        CancellationToken ct = default);
}
