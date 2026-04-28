using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Contracts.Orcamentos.Queries;
using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Filters;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/orcamentos")]
[Authorize]
[RequiresEstabelecimento]
public class OrcamentoController : ControllerBase
{
    private readonly ICommandBus _cmd;
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public OrcamentoController(ICommandBus cmd, IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _cmd = cmd;
        _query = query;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrcamentoResumoDto>>> Listar(
        [FromQuery] long? pacienteId,
        [FromQuery] string? status)
    {
        var result = await _query.Query<ListarOrcamentosQuery, IEnumerable<OrcamentoResumoDto>>(
            new ListarOrcamentosQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                PacienteId = pacienteId,
                Status = status
            });
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<OrcamentoDto>> Obter(long id)
    {
        var result = await _query.Query<ObterOrcamentoQuery, OrcamentoDto>(
            new ObterOrcamentoQuery
            {
                OrcamentoId = id,
                EstabelecimentoId = _tenant.EstabelecimentoId
            });
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> Criar([FromBody] CriarOrcamentoDto dto)
    {
        var cmd = new CriarOrcamentoCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            PacienteId = dto.PacienteId,
            Validade = dto.Validade,
            Observacoes = dto.Observacoes,
            CriadoPorUsuarioId = _tenant.UsuarioId,
            Itens = dto.Itens.Select(i =>
                new ItemOrcamentoPayload(i.Descricao, i.Quantidade, i.ValorUnitario, i.DescontoPercent)).ToList()
        };

        await _cmd.Send(cmd);
        return CreatedAtAction(nameof(Obter), new { id = cmd.OrcamentoIdCriado },
            new { orcamentoId = cmd.OrcamentoIdCriado });
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult> Atualizar(long id, [FromBody] AtualizarOrcamentoDto dto)
    {
        await _cmd.Send(new AtualizarOrcamentoCommand
        {
            OrcamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Validade = dto.Validade,
            Observacoes = dto.Observacoes,
            Itens = dto.Itens.Select(i =>
                new ItemOrcamentoPayload(i.Descricao, i.Quantidade, i.ValorUnitario, i.DescontoPercent)).ToList()
        });
        return NoContent();
    }

    [HttpPost("{id:long}/aprovar")]
    public async Task<ActionResult> Aprovar(long id)
    {
        await _cmd.Send(new AprovarOrcamentoCommand
        {
            OrcamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    [HttpPost("{id:long}/recusar")]
    public async Task<ActionResult> Recusar(long id)
    {
        await _cmd.Send(new RecusarOrcamentoCommand
        {
            OrcamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }
}

// DTOs de entrada
public record ItemOrcamentoInputDto(
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal DescontoPercent);

public record CriarOrcamentoDto(
    long PacienteId,
    DateOnly Validade,
    string? Observacoes,
    List<ItemOrcamentoInputDto> Itens);

public record AtualizarOrcamentoDto(
    DateOnly Validade,
    string? Observacoes,
    List<ItemOrcamentoInputDto> Itens);
