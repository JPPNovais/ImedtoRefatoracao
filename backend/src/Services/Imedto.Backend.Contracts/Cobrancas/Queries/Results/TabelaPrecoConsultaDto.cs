namespace Imedto.Backend.Contracts.Cobrancas.Queries.Results;

public class TabelaPrecoConsultaDto
{
    public long Id { get; set; }
    /// <summary>null = preço padrão do estabelecimento.</summary>
    public Guid? ProfissionalId { get; set; }
    public string? ProfissionalNome { get; set; }
    public decimal ValorSugerido { get; set; }
    public bool Ativo { get; set; }
}
