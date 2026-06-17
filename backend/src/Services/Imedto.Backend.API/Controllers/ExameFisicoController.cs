using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Exame físico — body map + regiões anatômicas. Vinculado a uma evolução do prontuário.
/// Acesso restrito a Profissional/Dono (dado clínico sensível, fora do alcance de Recepcionista).
///
/// Roteamento:
/// - <c>POST /api/evolucoes/{evolucaoId}/exame-fisico</c> — registrar exame para uma evolução.
/// - <c>PUT /api/exame-fisico/{id}</c> — atualizar dados gerais + regiões.
/// - <c>GET /api/exame-fisico/{id}</c> — obter exame completo (com regiões).
/// - <c>GET /api/evolucoes/{evolucaoId}/exame-fisico</c> — exame da evolução (no fluxo do prontuário).
/// - <c>GET /api/pacientes/{pacienteId}/exames-fisicos</c> — lista paginada (resumida).
/// - <c>GET /api/pacientes/{pacienteId}/exames-fisicos/timeline</c> — N últimos.
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
[FeatureGate(Features.ExameFisico)]
[ApiController]
[Produces("application/json")]
public class ExameFisicoController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public ExameFisicoController(
        ICommandBus commandBus,
        IRequestBus requestBus,
        ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    /// <summary>Registra um novo exame físico em uma evolução existente.</summary>
    [HttpPost("api/evolucoes/{evolucaoId:long}/exame-fisico")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Registrar(long evolucaoId, [FromBody] RegistrarExameFisicoRequest request)
    {
        var command = new RegistrarExameFisicoCommand
        {
            EvolucaoId = evolucaoId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            AutorUsuarioId = _tenant.UsuarioId,
            RealizadoEm = request.RealizadoEm,
            DadosGeraisJson = request.DadosGeraisJson,
            ObservacoesGerais = request.ObservacoesGerais,
            Regioes = request.Regioes ?? Array.Empty<RegiaoExameFisicoInput>()
        };

        await _commandBus.Send(command);

        return StatusCode(StatusCodes.Status201Created, new { exameFisicoId = command.ExameFisicoIdCriado });
    }

    /// <summary>Atualiza um exame físico (dados gerais + regiões).</summary>
    [HttpPut("api/exame-fisico/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(long id, [FromBody] AtualizarExameFisicoRequest request)
    {
        await _commandBus.Send(new AtualizarExameFisicoCommand
        {
            ExameFisicoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            AutorUsuarioId = _tenant.UsuarioId,
            DadosGeraisJson = request.DadosGeraisJson,
            ObservacoesGerais = request.ObservacoesGerais,
            Regioes = request.Regioes ?? Array.Empty<RegiaoExameFisicoInput>()
        });
        return NoContent();
    }

    /// <summary>Retorna exame físico completo (com regiões).</summary>
    [HttpGet("api/exame-fisico/{id:long}")]
    [ProducesResponseType(typeof(ExameFisicoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(long id)
    {
        var dto = await _requestBus.Query<ObterExameFisicoQuery, ExameFisicoDto?>(new ObterExameFisicoQuery
        {
            ExameFisicoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId
        });

        if (dto is null) return NotFound();
        return Ok(dto);
    }

    /// <summary>Exame físico associado a uma evolução. 404 se a evolução não tem exame.</summary>
    [HttpGet("api/evolucoes/{evolucaoId:long}/exame-fisico")]
    [ProducesResponseType(typeof(ExameFisicoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorEvolucao(long evolucaoId)
    {
        var dto = await _requestBus.Query<ObterExameFisicoPorEvolucaoQuery, ExameFisicoDto?>(new ObterExameFisicoPorEvolucaoQuery
        {
            EvolucaoId = evolucaoId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId
        });

        if (dto is null) return NotFound();
        return Ok(dto);
    }

    /// <summary>Lista paginada (resumida — sem regiões).</summary>
    [HttpGet("api/pacientes/{pacienteId:long}/exames-fisicos")]
    [ProducesResponseType(typeof(PaginaExamesFisicosDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        long pacienteId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10)
    {
        var dto = await _requestBus.Query<ListarExamesFisicosDoPacienteQuery, PaginaExamesFisicosDto>(
            new ListarExamesFisicosDoPacienteQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId,
                Pagina = pagina,
                Tamanho = tamanho
            });
        return Ok(dto);
    }

    /// <summary>Timeline curta (N últimos, default 10, max 50).</summary>
    [HttpGet("api/pacientes/{pacienteId:long}/exames-fisicos/timeline")]
    [ProducesResponseType(typeof(IEnumerable<ExameFisicoResumoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Timeline(long pacienteId, [FromQuery] int ate = 10)
    {
        var lista = await _requestBus.Query<TimelineExamesFisicosQuery, IEnumerable<ExameFisicoResumoDto>>(
            new TimelineExamesFisicosQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId,
                Ate = ate
            });
        return Ok(lista);
    }
}

public record RegistrarExameFisicoRequest(
    DateTime? RealizadoEm,
    string? DadosGeraisJson,
    string? ObservacoesGerais,
    IEnumerable<RegiaoExameFisicoInput>? Regioes);

public record AtualizarExameFisicoRequest(
    string? DadosGeraisJson,
    string? ObservacoesGerais,
    IEnumerable<RegiaoExameFisicoInput>? Regioes);
