namespace Imedto.Backend.Contracts.Receitas.Queries.Results;

public class ConfiguracaoReceitaDto
{
    public long EstabelecimentoId { get; set; }
    public string? CabecalhoHtml { get; set; }
    public string? RodapeHtml { get; set; }
    public long? ModeloPadraoId { get; set; }
    public string? EmissorPadrao { get; set; }
    public DateTime? AtualizadaEm { get; set; }
}

public class MedicamentoFavoritoDto
{
    public long Id { get; set; }
    public string Medicamento { get; set; } = string.Empty;
    public string? Posologia { get; set; }
    public string? Via { get; set; }
    public int UsoCount { get; set; }
    public DateTime? UltimoUso { get; set; }
}
