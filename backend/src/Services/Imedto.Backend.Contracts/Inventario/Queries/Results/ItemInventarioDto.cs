namespace Imedto.Backend.Contracts.Inventario.Queries.Results;

public class PaginaItensInventarioDto
{
    public IEnumerable<ItemInventarioDto> Itens { get; set; } = Array.Empty<ItemInventarioDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}

public class PaginaMovimentacoesEstoqueDto
{
    public IEnumerable<MovimentacaoEstoqueDto> Itens { get; set; } = Array.Empty<MovimentacaoEstoqueDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}

public class ItemInventarioDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    /// <summary>FK para CategoriaEstoque — front usa para reabrir o modal de edição com a categoria correta.</summary>
    public long? CategoriaId { get; set; }
    public string? CategoriaCor { get; set; }
    public string? CategoriaIcone { get; set; }
    public long? FabricanteId { get; set; }
    public string? FabricanteNome { get; set; }
    public long? FornecedorPadraoId { get; set; }
    public string? FornecedorPadraoNome { get; set; }
    public long? LocalPadraoId { get; set; }
    public string? LocalPadraoNome { get; set; }
    public string UnidadeMedida { get; set; } = string.Empty;
    public decimal QuantidadeAtual { get; set; }
    public decimal QuantidadeMinima { get; set; }
    public decimal CustoMedio { get; set; }
    public decimal? CustoUnitario { get; set; }
    public bool EstoqueAbaixoMinimo { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public class MovimentacaoEstoqueDto
{
    public long Id { get; set; }
    public long ItemInventarioId { get; set; }
    public string ItemNome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal QuantidadeAnterior { get; set; }
    public decimal QuantidadeApos { get; set; }
    public decimal CustoUnitario { get; set; }
    public decimal CustoTotal { get; set; }
    public string? Observacao { get; set; }
    public string UsuarioNome { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}
