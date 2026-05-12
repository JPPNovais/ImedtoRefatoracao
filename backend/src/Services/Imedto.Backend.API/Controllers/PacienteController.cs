using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Gerenciamento de pacientes do estabelecimento ativo. Todos os endpoints exigem o
/// header <c>X-Estabelecimento-Id</c> via <see cref="RequiresEstabelecimentoAttribute"/>.
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[RequiresAssinaturaAtiva]
[RequiresAcao("pacientes")]
[ApiController]
[Route("api/paciente")]
[Produces("application/json")]
public class PacienteController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public PacienteController(
        ICommandBus commandBus,
        IRequestBus requestBus,
        ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    /// <summary>Lista paginada de pacientes do estabelecimento atual, com busca por nome ou CPF.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaPacientesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string busca = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20)
    {
        var pagina1 = await _requestBus.Query<ListarPacientesQuery, PaginaPacientesDto>(
            new ListarPacientesQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Busca = busca,
                Pagina = pagina,
                TamanhoPagina = tamanho
            });

        return Ok(pagina1);
    }

    /// <summary>KPIs agregados (total + novos no mês) — usados no header da lista.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(PacienteStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Stats()
    {
        var dto = await _requestBus.Query<ObterPacienteStatsQuery, PacienteStatsDto>(
            new ObterPacienteStatsQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(dto);
    }

    /// <summary>Retorna os dados de um paciente.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(PacienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(long id)
    {
        var dto = await _requestBus.Query<ObterPacienteQuery, PacienteDto>(
            new ObterPacienteQuery
            {
                PacienteId = id,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId
            });

        if (dto is null) return NotFound();
        return Ok(dto);
    }

    /// <summary>Cadastra um novo paciente no estabelecimento atual. Apenas Profissional ou Dono.</summary>
    [HttpPost]
    [RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] PacienteRequest request)
    {
        await _commandBus.Send(new CadastrarPacienteCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            NomeCompleto = request.NomeCompleto,
            Cpf = request.Cpf,
            DocumentoInternacional = request.DocumentoInternacional,
            DataNascimento = request.DataNascimento,
            Genero = request.Genero,
            Telefone = request.Telefone,
            Email = request.Email,
            Endereco = request.Endereco,
            Observacoes = request.Observacoes,
            Tags = request.Tags ?? Array.Empty<string>(),
            Alertas = request.Alertas ?? Array.Empty<string>(),
        });

        return Created(string.Empty, null);
    }

    /// <summary>Atualiza os dados de um paciente. Apenas Profissional ou Dono (Observacoes pode ter dado clinico).</summary>
    [HttpPut("{id:long}")]
    [RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(long id, [FromBody] PacienteRequest request)
    {
        await _commandBus.Send(new AtualizarPacienteCommand
        {
            PacienteId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            NomeCompleto = request.NomeCompleto,
            Cpf = request.Cpf,
            DocumentoInternacional = request.DocumentoInternacional,
            DataNascimento = request.DataNascimento,
            Genero = request.Genero,
            Telefone = request.Telefone,
            Email = request.Email,
            Endereco = request.Endereco,
            Observacoes = request.Observacoes,
            Tags = request.Tags ?? Array.Empty<string>(),
            Alertas = request.Alertas ?? Array.Empty<string>(),
        });

        return NoContent();
    }

    /// <summary>Soft delete (LGPD). Mantém registro marcado por retenção legal. Apenas Profissional ou Dono.</summary>
    [HttpDelete("{id:long}")]
    [RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deletar(long id)
    {
        await _commandBus.Send(new DeletarPacienteCommand
        {
            PacienteId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId
        });

        return NoContent();
    }

    /// <summary>LGPD Art. 18 — exporta todos os dados pessoais do paciente em JSON. Apenas Dono (acesso a TODA PII do titular).</summary>
    [HttpGet("{id:long}/exportar-dados")]
    [RequiresPapel(TenantPapel.Dono)]
    [ProducesResponseType(typeof(PacienteExportLgpdDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ExportarDados(long id)
    {
        var dados = await _requestBus.Query<ExportarDadosPacienteQuery, PacienteExportLgpdDto>(
            new ExportarDadosPacienteQuery
            {
                PacienteId = id,
                EstabelecimentoId = _tenant.EstabelecimentoId
            });

        return Ok(dados);
    }
}

public record PacienteRequest(
    string NomeCompleto,
    string Cpf,
    string DocumentoInternacional,
    [property: System.Text.Json.Serialization.JsonConverter(typeof(Imedto.Backend.SharedKernel.Json.DateOnlyAsYmdJsonConverter))]
    DateTime? DataNascimento,
    string Genero,
    string Telefone,
    string Email,
    string Endereco,
    string Observacoes,
    IReadOnlyList<string> Tags = null,
    IReadOnlyList<string> Alertas = null);
