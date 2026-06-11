namespace Imedto.Backend.Contracts.Financeiro.Queries.Results;

/// <summary>
/// Resultado da ExportarExtratoQuery: linhas completas + metadados para audit (CA10).
/// </summary>
public class ExportarExtratoResultDto
{
    public IReadOnlyList<LancamentoExtratoDto> Itens { get; set; } = Array.Empty<LancamentoExtratoDto>();
    public int TotalLinhas { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }
}
