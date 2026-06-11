namespace Imedto.Backend.Contracts.Orcamentos.Catalogos.Queries.Results;

public class CatalogoCirurgiaDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorBase { get; set; }
    public int? DuracaoPadraoMinutos { get; set; }
    public string? CodigoInterno { get; set; }
    public string? CodigoTuss { get; set; }
    public string? Categoria { get; set; }
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
    /// <summary>Tipo do local cirúrgico (5 valores: IntLocal/IntPeridural/IntGeral/SemInternacao/Ambulatorio).</summary>
    public string TipoLocal { get; set; } = string.Empty;
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
    /// <summary>Vínculo opcional com item de inventário (F4/addendum).</summary>
    public long? ItemInventarioId { get; set; }
    /// <summary>Nome snapshot do item de inventário vinculado (null se sem vínculo).</summary>
    public string? ItemInventarioNome { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal? ValorReferencia { get; set; }
    public bool UsoUnico { get; set; }
    public string Tipo { get; set; } = "Outros";
    public string? Marca { get; set; }
    public string Unidade { get; set; } = "un";
    public string? FornecedorNome { get; set; }
    public string? CodigoSku { get; set; }
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
    public bool Incluido { get; set; }
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

public class OrcamentoTeamRoleDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Papel { get; set; } = string.Empty;
    public Guid? ProfissionalUsuarioId { get; set; }
    public string? ProfissionalNome { get; set; }
    public string? NomePadrao { get; set; }
    public string TipoHonorario { get; set; } = "Percentual";
    public decimal Valor { get; set; }
    public string BaseCalculo { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? AtualizadaEm { get; set; }
}

public class OrcamentoAnestesistaFaixaDto
{
    public long Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int Ordem { get; set; }
}

/// <summary>
/// DTO de detalhe (GET /anestesistas/{id}). Inclui PII (telefone) para edição.
/// </summary>
public class OrcamentoAnestesistaDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid? ProfissionalUsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Crm { get; set; }
    public string? Especialidade { get; set; }
    public string? Telefone { get; set; }
    public string? TabelaHonorarios { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? AtualizadaEm { get; set; }
    public List<OrcamentoAnestesistaFaixaDto> Faixas { get; set; } = new();
}

/// <summary>
/// DTO de listagem (GET /anestesistas). LGPD: NÃO expõe telefone — a tela de
/// listagem não exibe esse campo. Para editar, o front faz GET /anestesistas/{id}
/// e recebe o <see cref="OrcamentoAnestesistaDto"/>.
/// </summary>
public class OrcamentoAnestesistaListaDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid? ProfissionalUsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Crm { get; set; }
    public string? Especialidade { get; set; }
    public string? TabelaHonorarios { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? AtualizadaEm { get; set; }
    public List<OrcamentoAnestesistaFaixaDto> Faixas { get; set; } = new();
}

public class OrcamentoPacoteResumoDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public long? AnestesistaId { get; set; }
    public string? AnestesistaNome { get; set; }
    public decimal? ValorTotalSugerido { get; set; }
    public bool Ativo { get; set; }
    public int TotalProcedimentos { get; set; }
    public int TotalProdutos { get; set; }
    public int TotalTeamRoles { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? AtualizadaEm { get; set; }
}

public class OrcamentoPacoteProcedimentoDto
{
    public long CatalogoCirurgiaId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int Ordem { get; set; }
}

public class OrcamentoPacoteProdutoDto
{
    public long CatalogoProdutoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
}

public class OrcamentoPacoteTeamRoleDto
{
    public long TeamRoleId { get; set; }
    public string Papel { get; set; } = string.Empty;
}

public class OrcamentoPacoteDetalheDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public long? AnestesistaId { get; set; }
    public string? AnestesistaNome { get; set; }
    public bool? AnestesistaAtivo { get; set; }
    public decimal? ValorTotalSugerido { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? AtualizadaEm { get; set; }
    public List<OrcamentoPacoteProcedimentoDto> Procedimentos { get; set; } = new();
    public List<OrcamentoPacoteProdutoDto> Produtos { get; set; } = new();
    public List<OrcamentoPacoteTeamRoleDto> TeamRoles { get; set; } = new();
}
