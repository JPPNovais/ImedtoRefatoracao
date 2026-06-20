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

/// <summary>
/// Medicamento favorito do profissional — retornado pelo endpoint
/// <c>GET /api/receitas/favoritos</c> reativado para consumo do app mobile.
/// </summary>
public class MedicamentoFavoritoDto
{
    public long Id { get; set; }
    public string Medicamento { get; set; } = string.Empty;
    public string? Posologia { get; set; }
    public string? ViaAdministracao { get; set; }
    /// <summary>Quantidade de vezes que o profissional prescreveu este medicamento.</summary>
    public int UsoCount { get; set; }
    public DateTime? UltimoUso { get; set; }
}
