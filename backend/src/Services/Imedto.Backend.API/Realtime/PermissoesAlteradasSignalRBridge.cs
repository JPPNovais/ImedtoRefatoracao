using Microsoft.AspNetCore.SignalR;
using Imedto.Backend.API.Hubs;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.API.Realtime;

/// <summary>
/// Empurra o evento <c>permissoes-alteradas</c> para o usuário afetado quando o modelo
/// de permissão do vínculo dele é trocado. O front escuta e re-busca as permissões via
/// <c>GET /api/tenant/me/permissoes</c> para atualizar a sidebar/UI sem reload.
///
/// Sem este evento, o profissional logado continuaria vendo os menus antigos até o
/// próximo refresh — o backend já bloqueia 403, mas a UX fica ruim (clica → erro).
/// </summary>
public class PermissoesAlteradasSignalRBridge : IEventHandler<VinculoModeloPermissaoAlteradoEvent>
{
    private readonly IHubContext<EstabelecimentoHub> _hub;
    private readonly ILogger<PermissoesAlteradasSignalRBridge> _logger;

    public PermissoesAlteradasSignalRBridge(
        IHubContext<EstabelecimentoHub> hub,
        ILogger<PermissoesAlteradasSignalRBridge> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Handle(VinculoModeloPermissaoAlteradoEvent domainEvent)
    {
        var grupo = $"usuario:{domainEvent.ProfissionalUsuarioId}";

        var payload = new
        {
            estabelecimentoId = domainEvent.EstabelecimentoId,
            ocorridoEm = domainEvent.OcorridoEm,
        };

        await _hub.Clients.Group(grupo).SendAsync("permissoes-alteradas", payload);

        _logger.LogInformation(
            "Evento permissoes-alteradas enviado: Usuario={UsuarioId} Estab={Estab} Modelo={ModeloId}",
            domainEvent.ProfissionalUsuarioId, domainEvent.EstabelecimentoId, domainEvent.NovoModeloPermissaoId);
    }
}
