using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Atestados.Commands;
using Imedto.Backend.Contracts.Atestados.Queries;
using Imedto.Backend.Contracts.Atestados.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Atestados emitidos pelo profissional para um paciente. Acesso restrito a
/// Profissional/Dono — Recepcionista não emite nem visualiza atestado (PII clínico).
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
[RequiresAcao("prescricao")]
[ApiController]
[Produces("application/json")]
public class AtestadoController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public AtestadoController(ICommandBus commandBus, IRequestBus requestBus, ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    /// <summary>Emite um novo atestado para um paciente.</summary>
    [HttpPost("api/pacientes/{pacienteId:long}/atestados")]
    [Idempotent]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Emitir(long pacienteId, [FromBody] EmitirAtestadoRequest request)
    {
        var cmd = new EmitirAtestadoCommand
        {
            PacienteId = pacienteId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            ProfissionalUsuarioId = _tenant.UsuarioId,
            Tipo = request.Tipo,
            DiasAfastamento = request.DiasAfastamento,
            Cid10 = request.Cid10,
            Conteudo = request.Conteudo,
        };
        await _commandBus.Send(cmd);
        return CreatedAtAction(nameof(Obter), new { id = cmd.AtestadoIdCriado },
            new { atestadoId = cmd.AtestadoIdCriado });
    }

    /// <summary>Lista paginada de atestados do paciente — ordenados desc por data.</summary>
    [HttpGet("api/pacientes/{pacienteId:long}/atestados")]
    [ProducesResponseType(typeof(PaginaAtestadosDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarDoPaciente(
        long pacienteId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20)
    {
        var dto = await _requestBus.Query<ListarAtestadosDoPacienteQuery, PaginaAtestadosDto>(
            new ListarAtestadosDoPacienteQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId,
                Pagina = pagina,
                TamanhoPagina = tamanho,
            });
        return Ok(dto);
    }

    /// <summary>Detalhe do atestado.</summary>
    [HttpGet("api/atestados/{id:long}")]
    [ProducesResponseType(typeof(AtestadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Obter(long id)
    {
        var dto = await _requestBus.Query<ObterAtestadoQuery, AtestadoDto>(new ObterAtestadoQuery
        {
            AtestadoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
        });
        return Ok(dto);
    }

    // ─── Modelos de atestado ──────────────────────────────────────────────────

    [HttpGet("api/modelos-atestado")]
    [ProducesResponseType(typeof(IReadOnlyList<ModeloAtestadoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarModelos()
    {
        var dto = await _requestBus.Query<ListarModelosAtestadoQuery, IReadOnlyList<ModeloAtestadoDto>>(
            new ListarModelosAtestadoQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(dto);
    }

    [HttpPost("api/modelos-atestado")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CriarModelo([FromBody] CriarModeloAtestadoRequest request)
    {
        var cmd = new CriarModeloAtestadoCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            ProfissionalUsuarioId = _tenant.UsuarioId,
            Nome = request.Nome,
            Tipo = request.Tipo,
            Conteudo = request.Conteudo,
        };
        await _commandBus.Send(cmd);
        return Created(string.Empty, new { modeloId = cmd.ModeloIdCriado });
    }

    [HttpPut("api/modelos-atestado/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AtualizarModelo(long id, [FromBody] CriarModeloAtestadoRequest request)
    {
        await _commandBus.Send(new AtualizarModeloAtestadoCommand
        {
            ModeloId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            Nome = request.Nome,
            Tipo = request.Tipo,
            Conteudo = request.Conteudo,
        });
        return NoContent();
    }

    [HttpDelete("api/modelos-atestado/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ExcluirModelo(long id)
    {
        await _commandBus.Send(new ExcluirModeloAtestadoCommand
        {
            ModeloId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
        });
        return NoContent();
    }
}

public record EmitirAtestadoRequest(
    string Tipo,
    int? DiasAfastamento,
    string? Cid10,
    string Conteudo);

public record CriarModeloAtestadoRequest(
    string Nome,
    string Tipo,
    string Conteudo);
