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
    /// Remove o conteúdo do estabelecimento conforme os módulos selecionados (hard delete bypass
    /// do SoftDeleteInterceptor). Mantém a casca (<c>estabelecimentos</c> não é deletado).
    /// Registra auditoria obrigatória. Após o delete de <c>Configuracoes</c>, recria os seeds
    /// padrão (modelos de permissão + financeiro).
    /// </summary>
    /// <param name="estabelecimentoId">Id do estabelecimento a resetar.</param>
    /// <param name="modulos">Módulos a apagar. Se omitido no controller, usa <see cref="ResetModulos.Tudo"/>.</param>
    /// <param name="motivo">Motivo do reset (obrigatório para auditoria).</param>
    /// <param name="executadoPorUsuarioId">Id do admin que executou.</param>
    Task ResetEstabelecimentoAsync(
        long estabelecimentoId,
        ResetModulos modulos,
        string motivo,
        Guid executadoPorUsuarioId,
        CancellationToken ct = default);
}
