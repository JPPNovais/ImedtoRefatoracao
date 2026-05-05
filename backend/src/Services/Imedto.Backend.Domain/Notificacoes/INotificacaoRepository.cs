namespace Imedto.Backend.Domain.Notificacoes;

/// <summary>
/// Repositório de escrita de <see cref="Notificacao"/>. Listagens vão pelo
/// <c>INotificacaoQueryRepository</c> (Dapper) — separação CQRS.
/// </summary>
public interface INotificacaoRepository
{
    /// <summary>
    /// Carrega a notificação filtrando por <paramref name="usuarioId"/>
    /// (Notificacao é per-usuario — defense-in-depth IDOR/LGPD).
    /// Retorna null se inexistente ou de outro usuário.
    /// </summary>
    Task<Notificacao?> ObterPorIdOuNulo(long id, Guid usuarioId);

    Task Salvar(Notificacao notificacao);

    /// <summary>
    /// Marca todas as notificações não-lidas do usuário como lidas em uma única operação SQL.
    /// Retorna a quantidade afetada (útil para retorno informativo no endpoint).
    /// </summary>
    Task<int> MarcarTodasLidasDoUsuario(Guid usuarioId);
}
