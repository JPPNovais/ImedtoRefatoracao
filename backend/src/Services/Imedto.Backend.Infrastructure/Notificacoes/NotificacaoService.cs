using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Infrastructure.Notificacoes;

/// <summary>
/// Implementação única de <see cref="INotificacaoService"/>. Persiste o aggregate,
/// só então anexa o <see cref="Events.NotificacaoCriadaEvent"/> (precisa do Id) e publica
/// no <c>IEventBus</c>. Mesmo padrão dos handlers que usam <c>MarcarComoCriado</c>/<c>MarcarComoConvidado</c>.
///
/// LGPD: não loga título/mensagem completos — só id, usuário e categoria. Mensagens
/// podem conter dados pessoais (nome de paciente em lembrete, valor financeiro etc.).
/// </summary>
public class NotificacaoService : INotificacaoService
{
    private readonly INotificacaoRepository _repo;
    private readonly IEventBus _eventBus;
    private readonly ILogger<NotificacaoService> _logger;

    public NotificacaoService(
        INotificacaoRepository repo,
        IEventBus eventBus,
        ILogger<NotificacaoService> logger)
    {
        _repo = repo;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task EnviarAsync(
        Guid usuarioId,
        long? estabelecimentoId,
        string titulo,
        string mensagem,
        CategoriaNotificacao categoria,
        string? linkAcao = null,
        CancellationToken ct = default)
    {
        var notificacao = Notificacao.Criar(usuarioId, estabelecimentoId, titulo, mensagem, categoria, linkAcao);

        await _repo.Salvar(notificacao);  // popula Id
        notificacao.MarcarComoCriada();    // anexa evento agora que Id é > 0

        foreach (var evt in notificacao.DomainEvents)
            await _eventBus.Publish(evt);

        notificacao.ClearDomainEvents();

        _logger.LogInformation(
            "Notificação criada: Id={Id}, Usuario={UsuarioId}, Categoria={Categoria}",
            notificacao.Id, usuarioId, categoria);
    }
}
