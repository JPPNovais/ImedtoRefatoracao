namespace Imedto.Backend.Contracts.Inventario.Queries.Results;

public class ItemInventarioDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string UnidadeMedida { get; set; } = string.Empty;
    public decimal QuantidadeAtual { get; set; }
    public decimal QuantidadeMinima { get; set; }
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
    public string? Observacao { get; set; }
    public string UsuarioNome { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}
