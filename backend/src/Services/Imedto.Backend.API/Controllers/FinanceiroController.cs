using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
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
[RequiresAcao("financeiro")]
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
    public async Task<ActionResult<PaginaLancamentosDto>> Listar(
        [FromQuery] string? tipo,
        [FromQuery] string? status,
        [FromQuery] string? categoria,
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20)
    {
        var result = await _query.Query<ListarLancamentosQuery, PaginaLancamentosDto>(
            new ListarLancamentosQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Tipo = tipo,
                Status = status,
                Categoria = categoria,
                DataInicio = dataInicio,
                DataFim = dataFim,
                Pagina = pagina,
                TamanhoPagina = tamanho
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
    [Idempotent]
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

    // -------------------- Categorias financeiras --------------------

    [HttpGet("categorias")]
    public async Task<ActionResult<IEnumerable<CategoriaFinanceiraDto>>> ListarCategorias(
        [FromQuery] string? tipo,
        [FromQuery] bool? ativas,
        [FromQuery] bool? padrao)
    {
        var result = await _query.Query<ListarCategoriasFinanceirasQuery, IEnumerable<CategoriaFinanceiraDto>>(
            new ListarCategoriasFinanceirasQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Tipo = tipo,
                Ativas = ativas,
                Padrao = padrao
            });
        return Ok(result);
    }

    [HttpPost("categorias")]
    public async Task<ActionResult> CriarCategoria([FromBody] CriarCategoriaFinanceiraDto dto)
    {
        await _cmd.Send(new CriarCategoriaFinanceiraCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome,
            Tipo = dto.Tipo
        });
        return NoContent();
    }

    [HttpPut("categorias/{id:long}")]
    public async Task<ActionResult> AtualizarCategoria(long id, [FromBody] AtualizarCategoriaFinanceiraDto dto)
    {
        await _cmd.Send(new AtualizarCategoriaFinanceiraCommand
        {
            CategoriaId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome,
            Tipo = dto.Tipo
        });
        return NoContent();
    }

    [HttpPost("categorias/{id:long}/inativar")]
    public async Task<ActionResult> InativarCategoria(long id)
    {
        await _cmd.Send(new InativarCategoriaFinanceiraCommand
        {
            CategoriaId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    // -------------------- Formas de pagamento --------------------

    [HttpGet("formas-pagamento")]
    public async Task<ActionResult<IEnumerable<FormaPagamentoDto>>> ListarFormasPagamento(
        [FromQuery] bool? ativas,
        [FromQuery] bool? padrao)
    {
        var result = await _query.Query<ListarFormasPagamentoQuery, IEnumerable<FormaPagamentoDto>>(
            new ListarFormasPagamentoQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Ativas = ativas,
                Padrao = padrao
            });
        return Ok(result);
    }

    [HttpPost("formas-pagamento")]
    public async Task<ActionResult> CriarFormaPagamento([FromBody] CriarFormaPagamentoDto dto)
    {
        await _cmd.Send(new CriarFormaPagamentoCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome
        });
        return NoContent();
    }

    [HttpPut("formas-pagamento/{id:long}")]
    public async Task<ActionResult> AtualizarFormaPagamento(long id, [FromBody] AtualizarFormaPagamentoDto dto)
    {
        await _cmd.Send(new AtualizarFormaPagamentoCommand
        {
            FormaPagamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome
        });
        return NoContent();
    }

    [HttpPost("formas-pagamento/{id:long}/inativar")]
    public async Task<ActionResult> InativarFormaPagamento(long id)
    {
        await _cmd.Send(new InativarFormaPagamentoCommand
        {
            FormaPagamentoId = id,
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

public record CriarCategoriaFinanceiraDto(string Nome, string Tipo);
public record AtualizarCategoriaFinanceiraDto(string Nome, string Tipo);
public record CriarFormaPagamentoDto(string Nome);
public record AtualizarFormaPagamentoDto(string Nome);
