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
[RequiresAssinaturaAtiva]  // 402 quando Trial expirado / Suspensa / Cancelada / Expirada
[RequiresAcao("agenda")]
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
    public async Task<ActionResult<PaginaAgendamentosDto>> Listar(
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        [FromQuery] Guid? profissionalUsuarioId,
        [FromQuery] long? pacienteId,
        [FromQuery] string? status,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10)
    {
        var result = await _query.Query<ListarAgendamentosQuery, PaginaAgendamentosDto>(
            new ListarAgendamentosQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim,
                ProfissionalUsuarioId = profissionalUsuarioId,
                PacienteId = pacienteId,
                Status = status,
                Pagina = pagina,
                TamanhoPagina = tamanho
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
        [FromQuery, Required] DateOnly dataFim,
        [FromQuery] int? duracaoMinutos = null)
    {
        if (dataFim < dataInicio)
            return BadRequest("dataFim deve ser maior ou igual a dataInicio.");
        if (dataFim > dataInicio.AddDays(30))
            return BadRequest("Intervalo máximo é de 30 dias.");
        if (duracaoMinutos is int d && (d < 5 || d > 480))
            return BadRequest("duracaoMinutos deve estar entre 5 e 480.");

        var result = await _query.Query<ConsultarDisponibilidadeQuery, DisponibilidadeSemanaDto>(
            new ConsultarDisponibilidadeQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                ProfissionalUsuarioId = profissionalUsuarioId,
                DataInicio = dataInicio,
                DataFim = dataFim,
                DuracaoMinutos = duracaoMinutos,
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
            Observacoes = dto.Observacoes,
            SalaId = dto.SalaId,
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

    [HttpPost("{id:long}/checkin")]
    public async Task<ActionResult> RegistrarCheckIn(long id, [FromBody] RegistrarCheckInDto? dto)
    {
        await _cmd.Send(new RegistrarCheckInAgendamentoCommand
        {
            AgendamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SalaId = dto?.SalaId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
            TipoAtendimento = dto?.TipoAtendimento ?? "Particular",
            ValorCobrado = dto?.ValorCobrado ?? 0m,
            // F6/R7: convenioId opcional (recepção pode não ter à mão — CA144)
            ConvenioId = dto?.ConvenioId,
        });
        return NoContent();
    }

    [HttpPut("{id:long}/sala")]
    public async Task<ActionResult> AlocarSala(long id, [FromBody] AlocarSalaDto dto)
    {
        await _cmd.Send(new AlocarSalaAgendamentoCommand
        {
            AgendamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SalaId = dto.SalaId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
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
    public async Task<ActionResult<PaginaListaEsperaDto>> ListarListaEspera(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10)
    {
        var data = await _query.Query<ListarListaEsperaQuery, PaginaListaEsperaDto>(
            new ListarListaEsperaQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Pagina = pagina,
                TamanhoPagina = tamanho
            });
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
    string? Observacoes,
    long? SalaId);

public record AtualizarAgendamentoDto(
    Guid ProfissionalUsuarioId,
    DateTime InicioPrevisto,
    DateTime FimPrevisto,
    string TipoServico,
    string? Observacoes);

public record CancelarAgendamentoDto(string Motivo);

public record RegistrarCheckInDto(long? SalaId, string TipoAtendimento = "Particular", decimal ValorCobrado = 0m, long? ConvenioId = null);

public record AlocarSalaDto(long? SalaId);
