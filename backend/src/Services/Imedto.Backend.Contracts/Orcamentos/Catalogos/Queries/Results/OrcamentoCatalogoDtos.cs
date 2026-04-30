namespace Imedto.Backend.Contracts.Orcamentos.Catalogos.Queries.Results;

public class CatalogoCirurgiaDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorBase { get; set; }
    public int? DuracaoPadraoMinutos { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? AtualizadaEm { get; set; }
}

public class ValorProfissionalOrcamentoDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid? ProfissionalUsuarioId { get; set; }
    public string? ProfissionalNome { get; set; }
    public string Funcao { get; set; } = string.Empty;
    public int TempoBaseMinutos { get; set; }
    public decimal ValorTempoBase { get; set; }
    public int TempoAdicionalMinutos { get; set; }
    public decimal ValorAdicional { get; set; }
    public decimal ValorPlus { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? AtualizadaEm { get; set; }
}

public class ConfiguracaoLocalCirurgiaDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string TipoInternacao { get; set; } = string.Empty;
    public int TempoBaseMinutos { get; set; }
    public decimal ValorBase { get; set; }
    public int TempoAdicionalMinutos { get; set; }
    public decimal ValorAdicional { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? AtualizadaEm { get; set; }
}

public class CatalogoEquipeEspecializadaDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorPadrao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? AtualizadaEm { get; set; }
}

public class CatalogoImplanteDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public long? ItemInventarioId { get; set; }
    public string? ItemInventarioNome { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal CustoUnitario { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? AtualizadaEm { get; set; }
}

public class CatalogoProdutoDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal? ValorReferencia { get; set; }
    public bool UsoUnico { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? AtualizadaEm { get; set; }
}

public class CatalogoCirurgiaProdutoDto
{
    public long Id { get; set; }
    public long CatalogoCirurgiaId { get; set; }
    public long CatalogoProdutoId { get; set; }
    public string ProdutoNome { get; set; } = string.Empty;
    public bool ProdutoUsoUnico { get; set; }
    public decimal? ProdutoValorReferencia { get; set; }
    public decimal QuantidadePadrao { get; set; }
    public bool Obrigatorio { get; set; }
    public DateTime CriadaEm { get; set; }
}

public class ConfiguracaoPagamentoCatalogoDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public long FormaPagamentoId { get; set; }
    public string? FormaPagamentoNome { get; set; }
    public decimal AcrescimoPercentual { get; set; }
    public decimal EntradaPercentualPadrao { get; set; }
    public decimal TaxaParcela { get; set; }
    public int ParcelasMaximas { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? AtualizadaEm { get; set; }
}
