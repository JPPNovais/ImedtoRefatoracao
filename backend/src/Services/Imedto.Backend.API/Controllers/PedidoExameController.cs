using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.PedidosExame.Commands;
using Imedto.Backend.Contracts.PedidosExame.Queries;
using Imedto.Backend.Contracts.PedidosExame.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Pedidos de exame emitidos pelo profissional para um paciente. PII clínico —
/// restrito a Profissional/Dono.
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
[RequiresAcao("prescricao")]
[ApiController]
[Produces("application/json")]
public class PedidoExameController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public PedidoExameController(ICommandBus commandBus, IRequestBus requestBus, ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    [HttpPost("api/pacientes/{pacienteId:long}/pedidos-exame")]
    [Idempotent]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Emitir(long pacienteId, [FromBody] EmitirPedidoExameRequest request)
    {
        var cmd = new EmitirPedidoExameCommand
        {
            PacienteId = pacienteId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            ProfissionalUsuarioId = _tenant.UsuarioId,
            Tipo = request.Tipo,
            Exames = request.Exames?.ToList() ?? new List<string>(),
            IndicacaoClinica = request.IndicacaoClinica,
            Cid10 = request.Cid10,
            Observacoes = request.Observacoes,
        };
        await _commandBus.Send(cmd);
        return CreatedAtAction(nameof(Obter), new { id = cmd.PedidoExameIdCriado },
            new { pedidoExameId = cmd.PedidoExameIdCriado });
    }

    [HttpGet("api/pacientes/{pacienteId:long}/pedidos-exame")]
    [ProducesResponseType(typeof(PaginaPedidosExameDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarDoPaciente(
        long pacienteId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10)
    {
        Enum.TryParse<TenantPapel>(_tenant.Papel, ignoreCase: true, out var papelListar);
        var dto = await _requestBus.Query<ListarPedidosExameDoPacienteQuery, PaginaPedidosExameDto>(
            new ListarPedidosExameDoPacienteQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId,
                SolicitantePapel = papelListar,
                Pagina = pagina,
                TamanhoPagina = tamanho,
            });
        return Ok(dto);
    }

    [HttpGet("api/pedidos-exame/{id:long}")]
    [ProducesResponseType(typeof(PedidoExameDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Obter(long id)
    {
        Enum.TryParse<TenantPapel>(_tenant.Papel, ignoreCase: true, out var papelObter);
        var dto = await _requestBus.Query<ObterPedidoExameQuery, PedidoExameDto>(new ObterPedidoExameQuery
        {
            PedidoExameId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            SolicitantePapel = papelObter,
        });
        return Ok(dto);
    }
}

public record EmitirPedidoExameRequest(
    string Tipo,
    IEnumerable<string>? Exames,
    string IndicacaoClinica,
    string? Cid10,
    string? Observacoes);
