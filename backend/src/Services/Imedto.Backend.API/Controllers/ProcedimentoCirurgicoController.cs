using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Cirurgias.Commands;
using Imedto.Backend.Contracts.Cirurgias.Queries;
using Imedto.Backend.Contracts.Cirurgias.Queries.Results;
using Imedto.Backend.Contracts.Cirurgias;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/procedimentos-cirurgicos")]
[Authorize]
[RequiresEstabelecimento]
[FeatureGate(Features.ProcedimentosCirurgicos)]
public class ProcedimentoCirurgicoController : ControllerBase
{
    private readonly ICommandBus _cmd;
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public ProcedimentoCirurgicoController(ICommandBus cmd, IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _cmd = cmd;
        _query = query;
        _tenant = tenant;
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProcedimentoCirurgicoDto>> Obter(long id)
    {
        var dto = await _query.Query<ObterProcedimentoQuery, ProcedimentoCirurgicoDto>(
            new ObterProcedimentoQuery
            {
                ProcedimentoId = id,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId
            });
        return Ok(dto);
    }

    [HttpGet("planejados")]
    public async Task<ActionResult<IEnumerable<ProcedimentoCirurgicoResumoDto>>> ListarPlanejados(
        [FromQuery] DateTime dataInicio,
        [FromQuery] DateTime dataFim)
    {
        if (dataFim < dataInicio)
            return BadRequest(new { mensagem = "Período inválido: dataFim anterior a dataInicio." });

        var lista = await _query.Query<ListarProcedimentosPlanejadosQuery, IEnumerable<ProcedimentoCirurgicoResumoDto>>(
            new ListarProcedimentosPlanejadosQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim
            });
        return Ok(lista);
    }

    [HttpPost]
    [Idempotent]
    public async Task<ActionResult> Planejar([FromBody] PlanejarProcedimentoDto dto)
    {
        var cmd = new PlanejarProcedimentoCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            PacienteId = dto.PacienteId,
            ProntuarioId = dto.ProntuarioId,
            AgendamentoId = dto.AgendamentoId,
            CirurgiaPrincipal = dto.CirurgiaPrincipal,
            CirurgiaCodigo = dto.CirurgiaCodigo,
            DataAgendada = dto.DataAgendada,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            EquipeInicial = dto.EquipeInicial?.Select(m =>
                new EquipeInicialPayload(m.ProfissionalUsuarioId, m.Papel)).ToList() ?? new()
        };

        await _cmd.Send(cmd);
        return CreatedAtAction(nameof(Obter), new { id = cmd.ProcedimentoIdCriado },
            new { procedimentoId = cmd.ProcedimentoIdCriado });
    }

    [HttpPost("{id:long}/confirmar")]
    public async Task<ActionResult> Confirmar(long id)
    {
        await _cmd.Send(new ConfirmarProcedimentoCommand
        {
            ProcedimentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId
        });
        return NoContent();
    }

    [HttpPost("{id:long}/realizar")]
    public async Task<ActionResult> Realizar(long id, [FromBody] RegistrarRealizacaoDto dto)
    {
        await _cmd.Send(new RegistrarRealizacaoCommand
        {
            ProcedimentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            DataRealizada = dto.DataRealizada,
            DescricaoCirurgica = dto.DescricaoCirurgica,
            FichaAnestesica = dto.FichaAnestesica,
            EvolucaoPosOp = dto.EvolucaoPosOp
        });
        return NoContent();
    }

    [HttpPost("{id:long}/cancelar")]
    public async Task<ActionResult> Cancelar(long id, [FromBody] CancelarProcedimentoDto dto)
    {
        await _cmd.Send(new CancelarProcedimentoCommand
        {
            ProcedimentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            Motivo = dto.Motivo
        });
        return NoContent();
    }

    [HttpPut("{id:long}/equipe")]
    public async Task<ActionResult> AtualizarEquipe(long id, [FromBody] AtualizarEquipeDto dto)
    {
        await _cmd.Send(new AtualizarEquipeCommand
        {
            ProcedimentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            Equipe = dto.Equipe?.Select(m =>
                new EquipeInicialPayload(m.ProfissionalUsuarioId, m.Papel)).ToList() ?? new()
        });
        return NoContent();
    }
}

[ApiController]
[Route("api/pacientes/{pacienteId:long}/procedimentos-cirurgicos")]
[Authorize]
[RequiresEstabelecimento]
public class ProcedimentosCirurgicosDoPacienteController : ControllerBase
{
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public ProcedimentosCirurgicosDoPacienteController(IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _query = query;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProcedimentoCirurgicoResumoDto>>> Listar(long pacienteId)
    {
        var lista = await _query.Query<ListarProcedimentosDoPacienteQuery, IEnumerable<ProcedimentoCirurgicoResumoDto>>(
            new ListarProcedimentosDoPacienteQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId
            });
        return Ok(lista);
    }
}

// DTOs de entrada
public record EquipeMembroInputDto(Guid ProfissionalUsuarioId, string Papel);

public record PlanejarProcedimentoDto(
    long PacienteId,
    long ProntuarioId,
    long? AgendamentoId,
    string CirurgiaPrincipal,
    string? CirurgiaCodigo,
    DateTime? DataAgendada,
    List<EquipeMembroInputDto>? EquipeInicial);

public record RegistrarRealizacaoDto(
    DateTime DataRealizada,
    string? DescricaoCirurgica,
    FichaAnestesica? FichaAnestesica,
    string? EvolucaoPosOp);

public record CancelarProcedimentoDto(string Motivo);

public record AtualizarEquipeDto(List<EquipeMembroInputDto>? Equipe);
