namespace Imedto.Backend.SharedKernel.Domain;

/// <summary>
/// Marca aggregates que NUNCA devem ser fisicamente excluídos do banco — exclusão é lógica.
/// Razões:
/// - LGPD: dados de saúde precisam ser retidos pelo período legal.
/// - Integridade contábil: histórico de movimentações financeiras é imutável.
/// - Auditoria: tentativas de hard delete são registradas em <c>audit_delete_attempts</c>.
///
/// O interceptor <c>SoftDeleteInterceptor</c> bloqueia qualquer <c>EntityState.Deleted</c>
/// nesta interface, força registro de auditoria e lança <see cref="BusinessException"/>.
/// Para deletar logicamente, use <see cref="MarcarComoDeletado"/>.
/// </summary>
public interface ISoftDeletable
{
    DateTime? DeletadoEm { get; }
    Guid? DeletadoPorUsuarioId { get; }

    /// <summary>
    /// Marca o aggregate como logicamente deletado. Não remove a linha — popula
    /// <see cref="DeletadoEm"/>/<see cref="DeletadoPorUsuarioId"/>. Repositórios de leitura
    /// devem filtrar <c>WHERE deletado_em IS NULL</c>.
    /// </summary>
    void MarcarComoDeletado(Guid usuarioId);
}
