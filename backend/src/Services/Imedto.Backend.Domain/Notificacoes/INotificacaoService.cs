namespace Imedto.Backend.Domain.Notificacoes;

/// <summary>
/// Porta única para criação de notificações in-app. Handlers de domínio (convite aceito,
/// agendamento criado, automações) devem chamar este serviço — nunca instanciar/persistir
/// <see cref="Notificacao"/> diretamente. A implementação cuida de:
/// <list type="bullet">
/// <item>Validações do aggregate.</item>
/// <item>Persistência via <c>INotificacaoRepository</c>.</item>
/// <item>Disparo de <see cref="Events.NotificacaoCriadaEvent"/> via <c>IEventBus</c> (para realtime/push).</item>
/// </list>
/// </summary>
public interface INotificacaoService
{
    Task EnviarAsync(
        Guid usuarioId,
        long? estabelecimentoId,
        string titulo,
        string mensagem,
        CategoriaNotificacao categoria,
        string? linkAcao = null,
        CancellationToken ct = default);
}
