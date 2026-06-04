using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Inventario.Commands;
using Imedto.Backend.Contracts.Inventario.Queries;
using Imedto.Backend.Contracts.Inventario.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Filters;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/inventario")]
[Authorize]
[RequiresEstabelecimento]
[RequiresAssinaturaAtiva]
[RequiresAcao("estoque")]
public class InventarioController : ControllerBase
{
    private readonly ICommandBus _cmd;
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public InventarioController(ICommandBus cmd, IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _cmd = cmd;
        _query = query;
        _tenant = tenant;
    }

    [HttpGet("itens")]
    public async Task<ActionResult<PaginaItensInventarioDto>> ListarItens(
        [FromQuery] string? categoria,
        [FromQuery] bool? apenasAbaixoMinimo,
        [FromQuery] bool? apenasAtivos,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20)
    {
        var result = await _query.Query<ListarItensInventarioQuery, PaginaItensInventarioDto>(
            new ListarItensInventarioQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Categoria = categoria,
                ApenasAbaixoMinimo = apenasAbaixoMinimo,
                ApenasAtivos = apenasAtivos ?? true,
                Pagina = pagina,
                TamanhoPagina = tamanho
            });
        return Ok(result);
    }

    [HttpPost("itens")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> CriarItem([FromBody] CriarItemInventarioDto dto)
    {
        var cmd = new CriarItemInventarioCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Codigo = dto.Codigo,
            Nome = dto.Nome,
            CategoriaId = dto.CategoriaId,
            FabricanteId = dto.FabricanteId,
            FornecedorPadraoId = dto.FornecedorPadraoId,
            LocalPadraoId = dto.LocalPadraoId,
            UnidadeMedida = dto.UnidadeMedida,
            QuantidadeInicial = dto.QuantidadeInicial,
            QuantidadeMinima = dto.QuantidadeMinima,
            CustoUnitarioInicial = dto.CustoUnitarioInicial,
            CustoUnitario = dto.CustoUnitario,
            CriadoPorUsuarioId = _tenant.UsuarioId
        };

        await _cmd.Send(cmd);
        return CreatedAtAction(nameof(ListarItens), new { }, new { itemId = cmd.ItemIdCriado });
    }

    [HttpPut("itens/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarItem(long id, [FromBody] AtualizarItemInventarioDto dto)
    {
        await _cmd.Send(new AtualizarItemInventarioCommand
        {
            ItemId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome,
            CategoriaId = dto.CategoriaId,
            FabricanteId = dto.FabricanteId,
            FornecedorPadraoId = dto.FornecedorPadraoId,
            LocalPadraoId = dto.LocalPadraoId,
            UnidadeMedida = dto.UnidadeMedida,
            QuantidadeMinima = dto.QuantidadeMinima,
            CustoUnitario = dto.CustoUnitario
        });
        return NoContent();
    }

    [HttpPost("itens/{id:long}/inativar")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> InativarItem(long id, [FromBody] InativarItemInventarioDto? dto = null)
    {
        await _cmd.Send(new InativarItemInventarioCommand
        {
            ItemId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioId = _tenant.UsuarioId,
            Observacao = dto?.Observacao
        });
        return NoContent();
    }

    [HttpGet("movimentacoes")]
    public async Task<ActionResult<PaginaMovimentacoesEstoqueDto>> ListarMovimentacoes(
        [FromQuery] long? itemInventarioId,
        [FromQuery] string? tipo,
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20)
    {
        var result = await _query.Query<ListarMovimentacoesQuery, PaginaMovimentacoesEstoqueDto>(
            new ListarMovimentacoesQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                ItemInventarioId = itemInventarioId,
                Tipo = tipo,
                DataInicio = dataInicio,
                DataFim = dataFim,
                Pagina = pagina,
                TamanhoPagina = tamanho
            });
        return Ok(result);
    }

    [HttpPost("movimentacoes")]
    [Idempotent]
    public async Task<ActionResult> RegistrarMovimentacao([FromBody] RegistrarMovimentacaoDto dto)
    {
        await _cmd.Send(new RegistrarMovimentacaoEstoqueCommand
        {
            ItemInventarioId = dto.ItemInventarioId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Tipo = dto.Tipo,
            Quantidade = dto.Quantidade,
            CustoUnitario = dto.CustoUnitario,
            Observacao = dto.Observacao,
            UsuarioId = _tenant.UsuarioId
        });
        return NoContent();
    }
}

public record CriarItemInventarioDto(
    string Codigo,
    string Nome,
    long CategoriaId,
    long? FabricanteId,
    long? FornecedorPadraoId,
    long? LocalPadraoId,
    string UnidadeMedida,
    decimal QuantidadeInicial,
    decimal QuantidadeMinima,
    decimal CustoUnitarioInicial,
    decimal? CustoUnitario);

public record AtualizarItemInventarioDto(
    string Nome,
    long CategoriaId,
    long? FabricanteId,
    long? FornecedorPadraoId,
    long? LocalPadraoId,
    string UnidadeMedida,
    decimal QuantidadeMinima,
    decimal? CustoUnitario);

public record RegistrarMovimentacaoDto(
    long ItemInventarioId,
    string Tipo,
    decimal Quantidade,
    decimal CustoUnitario,
    string? Observacao);

public record InativarItemInventarioDto(string? Observacao);
