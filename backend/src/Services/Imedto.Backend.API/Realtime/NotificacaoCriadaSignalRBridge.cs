using Microsoft.AspNetCore.SignalR;
using Imedto.Backend.API.Hubs;
using Imedto.Backend.Domain.Notificacoes.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.API.Realtime;

/// <summary>
/// Ponte entre o evento de domínio <see cref="NotificacaoCriadaEvent"/> e o transporte SignalR.
///
/// Vive na camada API porque depende de <c>IHubContext&lt;EstabelecimentoHub&gt;</c> — colocá-lo na
/// Infrastructure criaria referência circular (Infrastructure → API). É legítimo: o SignalR é
/// um detalhe de transporte, não de domínio.
///
/// LGPD: nunca loga título/mensagem (podem conter nome de paciente/valor). O log é só
/// metadados (id, usuário, categoria). O payload trafega cifrado pelo cookie HttpOnly + TLS
/// até o navegador do destinatário.
/// </summary>
public class NotificacaoCriadaSignalRBridge : IEventHandler<NotificacaoCriadaEvent>
{
    private readonly IHubContext<EstabelecimentoHub> _hub;
    private readonly ILogger<NotificacaoCriadaSignalRBridge> _logger;

    public NotificacaoCriadaSignalRBridge(
        IHubContext<EstabelecimentoHub> hub,
        ILogger<NotificacaoCriadaSignalRBridge> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Handle(NotificacaoCriadaEvent domainEvent)
    {
        var grupo = $"usuario:{domainEvent.UsuarioId}";

        // Payload espelha o NotificacaoDto do GET /api/notificacoes — frontend usa o mesmo shape
        // em ambas as fontes (REST inicial + push em tempo real).
        var payload = new
        {
            id = domainEvent.NotificacaoId,
            estabelecimentoId = domainEvent.EstabelecimentoId,
            titulo = domainEvent.Titulo,
            mensagem = domainEvent.Mensagem,
            categoria = domainEvent.Categoria.ToString(),
            linkAcao = domainEvent.LinkAcao,
            lida = false,
            criadaEm = domainEvent.OcorridoEm
        };

        await _hub.Clients.Group(grupo).SendAsync("notificacao-recebida", payload);

        // LGPD: NÃO logar titulo/mensagem. Só metadados.
        _logger.LogInformation(
            "Notificação enviada via SignalR: Id={Id} Usuario={UsuarioId} Categoria={Categoria}",
            domainEvent.NotificacaoId, domainEvent.UsuarioId, domainEvent.Categoria);
    }
}
