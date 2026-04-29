namespace Imedto.Backend.Domain.Notificacoes;

/// <summary>
/// Repositório de escrita de <see cref="Notificacao"/>. Listagens vão pelo
/// <c>INotificacaoQueryRepository</c> (Dapper) — separação CQRS.
/// </summary>
public interface INotificacaoRepository
{
    Task<Notificacao?> ObterPorIdOuNulo(long id);
    Task Salvar(Notificacao notificacao);

    /// <summary>
    /// Marca todas as notificações não-lidas do usuário como lidas em uma única operação SQL.
    /// Retorna a quantidade afetada (útil para retorno informativo no endpoint).
    /// </summary>
    Task<int> MarcarTodasLidasDoUsuario(Guid usuarioId);
}
