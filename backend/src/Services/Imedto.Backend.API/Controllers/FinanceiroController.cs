using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Filters;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/financeiro")]
[Authorize]
[RequiresEstabelecimento]
public class FinanceiroController : ControllerBase
{
    private readonly ICommandBus _cmd;
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public FinanceiroController(ICommandBus cmd, IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _cmd = cmd;
        _query = query;
        _tenant = tenant;
    }

    [HttpGet("lancamentos")]
    public async Task<ActionResult<IEnumerable<LancamentoDto>>> Listar(
        [FromQuery] string? tipo,
        [FromQuery] string? status,
        [FromQuery] string? categoria,
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim)
    {
        var result = await _query.Query<ListarLancamentosQuery, IEnumerable<LancamentoDto>>(
            new ListarLancamentosQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Tipo = tipo,
                Status = status,
                Categoria = categoria,
                DataInicio = dataInicio,
                DataFim = dataFim
            });
        return Ok(result);
    }

    [HttpGet("resumo")]
    public async Task<ActionResult<ResumoFinanceiroDto>> Resumo(
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim)
    {
        var result = await _query.Query<ObterResumoFinanceiroQuery, ResumoFinanceiroDto>(
            new ObterResumoFinanceiroQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim
            });
        return Ok(result);
    }

    [HttpPost("lancamentos")]
    public async Task<ActionResult> Criar([FromBody] CriarLancamentoDto dto)
    {
        await _cmd.Send(new CriarLancamentoCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Tipo = dto.Tipo,
            Descricao = dto.Descricao,
            Valor = dto.Valor,
            DataVencimento = dto.DataVencimento,
            Categoria = dto.Categoria,
            OrcamentoId = dto.OrcamentoId,
            CriadoPorUsuarioId = _tenant.UsuarioId
        });
        return NoContent();
    }

    [HttpPut("lancamentos/{id:long}")]
    public async Task<ActionResult> Atualizar(long id, [FromBody] AtualizarLancamentoDto dto)
    {
        await _cmd.Send(new AtualizarLancamentoCommand
        {
            LancamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Descricao = dto.Descricao,
            Valor = dto.Valor,
            DataVencimento = dto.DataVencimento,
            Categoria = dto.Categoria
        });
        return NoContent();
    }

    [HttpPost("lancamentos/{id:long}/pagar")]
    public async Task<ActionResult> Pagar(long id, [FromBody] PagarLancamentoDto? dto)
    {
        await _cmd.Send(new PagarLancamentoCommand
        {
            LancamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            DataPagamento = dto?.DataPagamento
        });
        return NoContent();
    }

    [HttpPost("lancamentos/{id:long}/cancelar")]
    public async Task<ActionResult> Cancelar(long id)
    {
        await _cmd.Send(new CancelarLancamentoCommand
        {
            LancamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }
}

public record CriarLancamentoDto(
    string Tipo,
    string Descricao,
    decimal Valor,
    DateOnly DataVencimento,
    string Categoria,
    long? OrcamentoId);

public record AtualizarLancamentoDto(
    string Descricao,
    decimal Valor,
    DateOnly DataVencimento,
    string Categoria);

public record PagarLancamentoDto(DateOnly? DataPagamento);
