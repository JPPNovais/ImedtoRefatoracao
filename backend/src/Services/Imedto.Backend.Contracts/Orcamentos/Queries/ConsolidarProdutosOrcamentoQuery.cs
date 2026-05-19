using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Queries;

/// <summary>
/// Consolida produtos das cirurgias selecionadas em um orçamento — aplica a regra
/// do legado (uso único: MAX entre cirurgias; não único: SOMA). Retorna a lista
/// pronta para exibição com nome do produto, quantidade efetiva, valor de
/// referência e origens (cirurgias que justificam a inclusão).
/// </summary>
public class ConsolidarProdutosOrcamentoQuery : IQuery<List<ProdutoConsolidadoDto>>
{
    public long EstabelecimentoId { get; set; }
    public List<CirurgiaSelecionadaPayload> Cirurgias { get; set; } = new();
}

public record CirurgiaSelecionadaPayload(long CatalogoCirurgiaId, int Quantidade);

public class ProdutoConsolidadoDto
{
    public long ProdutoId { get; set; }
    public string ProdutoNome { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public bool UsoUnico { get; set; }
    public List<long> OrigemCirurgiaIds { get; set; } = new();
    public List<string> OrigemCirurgiaNomes { get; set; } = new();
}
