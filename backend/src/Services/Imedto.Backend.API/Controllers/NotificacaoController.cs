using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Notificacoes.Commands;
using Imedto.Backend.Contracts.Notificacoes.Queries;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Notificações in-app do usuário autenticado. NÃO usa <c>[RequiresEstabelecimento]</c>:
/// a notificação é "do usuário", não "do estabelecimento" — o usuário precisa ver convites
/// e avisos de qualquer contexto (incluindo convites para estabelecimentos a que ainda não pertence).
///
/// Filtragem por <c>UsuarioId = User.Sub</c> é feita em todos os endpoints — o frontend não
/// passa o usuário e nem poderia (BFF: token é cookie HttpOnly). RLS no Postgres é defense-in-depth.
/// </summary>
[Authorize]
[ApiController]
[Route("api/notificacoes")]
[Produces("application/json")]
public class NotificacaoController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;

    public NotificacaoController(ICommandBus commandBus, IRequestBus requestBus)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
    }

    /// <summary>Lista paginada das notificações do usuário, ordenadas por mais recente.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaNotificacoesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] bool? lidas = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20)
    {
        var resultado = await _requestBus.Query<ListarNotificacoesQuery, PaginaNotificacoesDto>(
            new ListarNotificacoesQuery
            {
                UsuarioId = ObterUsuarioId(),
                Lidas = lidas,
                Pagina = pagina,
                Tamanho = tamanho
            });

        return Ok(resultado);
    }

    /// <summary>Marca uma notificação específica como lida (idempotente).</summary>
    [HttpPost("{id:long}/marcar-lida")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> MarcarLida(long id)
    {
        await _commandBus.Send(new MarcarNotificacaoLidaCommand
        {
            NotificacaoId = id,
            UsuarioId = ObterUsuarioId()
        });
        return NoContent();
    }

    /// <summary>Marca todas as notificações não-lidas do usuário como lidas.</summary>
    [HttpPost("marcar-todas-lidas")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarcarTodasLidas()
    {
        await _commandBus.Send(new MarcarTodasNotificacoesLidasCommand
        {
            UsuarioId = ObterUsuarioId()
        });
        return NoContent();
    }

    /// <summary>Contador de não-lidas — usado pelo badge do sino no header.</summary>
    [HttpGet("contador-nao-lidas")]
    [ProducesResponseType(typeof(ContadorNaoLidasDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ContadorNaoLidas()
    {
        var dto = await _requestBus.Query<ContadorNaoLidasQuery, ContadorNaoLidasDto>(
            new ContadorNaoLidasQuery { UsuarioId = ObterUsuarioId() });
        return Ok(dto);
    }

    private Guid ObterUsuarioId() => Guid.Parse(User.FindFirst("sub")!.Value);
}
