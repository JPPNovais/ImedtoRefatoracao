using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Imedto.Backend.API.Hubs;

/// <summary>
/// Hub de realtime do tenant. Cada conexão joina dois grupos potenciais:
///
/// 1. <c>usuario:{usuarioId}</c> — sempre, para entregas direcionadas ao usuário (ex: notificações in-app).
/// 2. <c>estab:{id}</c> — quando o cliente envia <c>X-Estabelecimento-Id</c> no handshake (header HTTP)
///    ou <c>estabelecimentoId</c> em query string (fallback WebSocket que não propaga headers).
///
/// LGPD: <see cref="OnConnectedAsync"/> não loga payloads — só identifica conexões pelo
/// <c>ConnectionId</c> e <c>UsuarioId</c> (claim sub do JWT). Mensagens (titulo/mensagem)
/// nunca devem ser logadas no servidor — elas trafegam apenas em memória até o cliente.
///
/// Single-instance: este hub vive em memória do processo. Em multi-instância seria
/// necessário um backplane (Redis). Marcado como TODO até cluster horizontal real.
/// </summary>
[Authorize]
public class EstabelecimentoHub : Hub
{
    private readonly ILogger<EstabelecimentoHub> _logger;

    public EstabelecimentoHub(ILogger<EstabelecimentoHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var http = Context.GetHttpContext();
        var usuarioId = Context.User?.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(usuarioId))
        {
            // Authorize já garantiria 401 antes de chegar aqui, mas defesa em profundidade:
            // sem sub, não joina nenhum grupo e o cliente não receberá nada.
            _logger.LogWarning("Conexão SignalR sem claim sub — encerrando. ConnectionId={ConnId}", Context.ConnectionId);
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"usuario:{usuarioId}");

        // Estabelecimento é opcional: o sino de notificações é por usuário, mas
        // outras features futuras (ex: presence em sala de atendimento) usarão estab:{id}.
        var estab = http?.Request.Headers["X-Estabelecimento-Id"].FirstOrDefault()
                    ?? http?.Request.Query["estabelecimentoId"].FirstOrDefault();

        if (long.TryParse(estab, out var estabelecimentoId) && estabelecimentoId > 0)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"estab:{estabelecimentoId}");

        _logger.LogInformation(
            "SignalR conectado: ConnectionId={ConnId} Usuario={UsuarioId} Estab={Estab}",
            Context.ConnectionId, usuarioId, estabelecimentoId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // SignalR remove a conexão dos grupos automaticamente — não precisamos limpar.
        if (exception is not null)
        {
            _logger.LogInformation(
                "SignalR desconectado com erro: ConnectionId={ConnId} Erro={Erro}",
                Context.ConnectionId, exception.Message);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
