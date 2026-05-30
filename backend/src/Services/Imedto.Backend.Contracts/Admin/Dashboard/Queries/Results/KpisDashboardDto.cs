namespace Imedto.Backend.Contracts.Admin.Dashboard.Queries.Results;

/// <summary>
/// KPIs do painel admin. POCO class — Dapper mapeia por nome de coluna.
/// Não usar record posicional: Dapper não suporta corretamente (bug com DateTimeOffset).
/// </summary>
public class KpisDashboardDto
{
    public int EstabelecimentosAtivos { get; set; }
    public int EstabelecimentosInativos { get; set; }
    public int AdminsAtivos { get; set; }
    public int TrialsEmAndamento { get; set; }
    public int TrialsExpirandoEm7Dias { get; set; }
    public int AssinaturasVigentes { get; set; }
    public int AssinaturasGratuitas { get; set; }
}
