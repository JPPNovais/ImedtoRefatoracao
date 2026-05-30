namespace Imedto.Backend.Contracts.Admin.Dashboard.Queries.Results;

/// <summary>
/// Ponto de crescimento mensal. POCO class para Dapper.
/// Mes no formato "YYYY-MM".
/// </summary>
public class CrescimentoMensalPontoDto
{
    public string Mes { get; set; } = string.Empty;
    public int Total { get; set; }
}
