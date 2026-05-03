using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Contracts.Agendamentos.Queries;
using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Filters;
using Imedto.Backend.SharedKernel.Tenancy;
using System.ComponentModel.DataAnnotations;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/agendamentos")]
[Authorize]
[RequiresEstabelecimento]
public class AgendamentoController : ControllerBase
{
    private readonly ICommandBus _cmd;
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public AgendamentoController(ICommandBus cmd, IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _cmd = cmd;
        _query = query;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AgendamentoDto>>> Listar(
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        [FromQuery] Guid? profissionalUsuarioId,
        [FromQuery] long? pacienteId,
        [FromQuery] string? status)
    {
        var result = await _query.Query<ListarAgendamentosQuery, IEnumerable<AgendamentoDto>>(
            new ListarAgendamentosQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim,
                ProfissionalUsuarioId = profissionalUsuarioId,
                PacienteId = pacienteId,
                Status = status
            });
        return Ok(result);
    }

    [HttpGet("contagem-por-dia")]
    public async Task<ActionResult<IEnumerable<ContagemPorDiaDto>>> ContarPorDia(
        [FromQuery, Required] DateOnly dataInicio,
        [FromQuery, Required] DateOnly dataFim,
        [FromQuery] Guid? profissionalUsuarioId)
    {
        if (dataFim < dataInicio)
            return BadRequest("dataFim deve ser maior ou igual a dataInicio.");
        if (dataFim > dataInicio.AddDays(60))
            return BadRequest("Intervalo máximo é de 60 dias.");

        var result = await _query.Query<ContarAgendamentosPorDiaQuery, IEnumerable<ContagemPorDiaDto>>(
            new ContarAgendamentosPorDiaQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim,
                ProfissionalUsuarioId = profissionalUsuarioId,
            });
        return Ok(result);
    }

    [HttpGet("disponibilidade")]
    public async Task<ActionResult<DisponibilidadeSemanaDto>> ConsultarDisponibilidade(
        [FromQuery, Required] Guid profissionalUsuarioId,
        [FromQuery, Required] DateOnly dataInicio,
        [FromQuery, Required] DateOnly dataFim)
    {
        if (dataFim < dataInicio)
            return BadRequest("dataFim deve ser maior ou igual a dataInicio.");
        if (dataFim > dataInicio.AddDays(30))
            return BadRequest("Intervalo máximo é de 30 dias.");

        var result = await _query.Query<ConsultarDisponibilidadeQuery, DisponibilidadeSemanaDto>(
            new ConsultarDisponibilidadeQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                ProfissionalUsuarioId = profissionalUsuarioId,
                DataInicio = dataInicio,
                DataFim = dataFim,
            });
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AgendamentoDto>> Obter(long id)
    {
        var result = await _query.Query<ObterAgendamentoQuery, AgendamentoDto>(
            new ObterAgendamentoQuery
            {
                AgendamentoId = id,
                EstabelecimentoId = _tenant.EstabelecimentoId
            });
        return Ok(result);
    }

    [HttpPost]
    [Idempotent]
    public async Task<ActionResult> Criar([FromBody] CriarAgendamentoDto dto)
    {
        var cmd = new CriarAgendamentoCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            PacienteId = dto.PacienteId,
            ProfissionalUsuarioId = dto.ProfissionalUsuarioId,
            CriadoPorUsuarioId = _tenant.UsuarioId,
            InicioPrevisto = dto.InicioPrevisto,
            FimPrevisto = dto.FimPrevisto,
            TipoServico = dto.TipoServico,
            Observacoes = dto.Observacoes
        };

        await _cmd.Send(cmd);
        return CreatedAtAction(nameof(Obter), new { id = cmd.AgendamentoIdCriado },
            new { agendamentoId = cmd.AgendamentoIdCriado });
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult> Atualizar(long id, [FromBody] AtualizarAgendamentoDto dto)
    {
        await _cmd.Send(new AtualizarAgendamentoCommand
        {
            AgendamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            ProfissionalUsuarioId = dto.ProfissionalUsuarioId,
            InicioPrevisto = dto.InicioPrevisto,
            FimPrevisto = dto.FimPrevisto,
            TipoServico = dto.TipoServico,
            Observacoes = dto.Observacoes
        });
        return NoContent();
    }

    [HttpPost("{id:long}/confirmar")]
    public async Task<ActionResult> Confirmar(long id)
    {
        await _cmd.Send(new ConfirmarAgendamentoCommand
        {
            AgendamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    [HttpPost("{id:long}/cancelar")]
    public async Task<ActionResult> Cancelar(long id, [FromBody] CancelarAgendamentoDto dto)
    {
        await _cmd.Send(new CancelarAgendamentoCommand
        {
            AgendamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Motivo = dto.Motivo
        });
        return NoContent();
    }

    [HttpPost("{id:long}/concluir")]
    public async Task<ActionResult> Concluir(long id)
    {
        await _cmd.Send(new ConcluirAgendamentoCommand
        {
            AgendamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    // ──────────── Lista de espera ────────────

    [HttpGet("lista-espera")]
    public async Task<ActionResult<IEnumerable<ListaEsperaItemDto>>> ListarListaEspera()
    {
        var data = await _query.Query<ListarListaEsperaQuery, IEnumerable<ListaEsperaItemDto>>(
            new ListarListaEsperaQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(data);
    }

    [HttpPost("lista-espera")]
    public async Task<ActionResult> AdicionarListaEspera([FromBody] AdicionarListaEsperaDto dto)
    {
        var cmd = new AdicionarListaEsperaCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            PacienteId = dto.PacienteId,
            Motivo = dto.Motivo,
            ProfissionalPreferidoId = dto.ProfissionalPreferidoId,
            Prioridade = dto.Prioridade ?? "Rotina",
            PreferenciaPeriodo = dto.PreferenciaPeriodo ?? "Qualquer",
            CriadoPorUsuarioId = _tenant.UsuarioId,
        };
        await _cmd.Send(cmd);
        return Ok(new { id = cmd.IdCriado });
    }

    [HttpDelete("lista-espera/{id:long}")]
    public async Task<ActionResult> RemoverListaEspera(long id)
    {
        await _cmd.Send(new RemoverListaEsperaCommand
        {
            Id = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
        });
        return NoContent();
    }
}

public record AdicionarListaEsperaDto(
    long PacienteId,
    string Motivo,
    Guid? ProfissionalPreferidoId,
    string? Prioridade,
    string? PreferenciaPeriodo);

// DTOs de entrada (request bodies)
public record CriarAgendamentoDto(
    long PacienteId,
    Guid ProfissionalUsuarioId,
    DateTime InicioPrevisto,
    DateTime FimPrevisto,
    string TipoServico,
    string? Observacoes);

public record AtualizarAgendamentoDto(
    Guid ProfissionalUsuarioId,
    DateTime InicioPrevisto,
    DateTime FimPrevisto,
    string TipoServico,
    string? Observacoes);

public record CancelarAgendamentoDto(string Motivo);
