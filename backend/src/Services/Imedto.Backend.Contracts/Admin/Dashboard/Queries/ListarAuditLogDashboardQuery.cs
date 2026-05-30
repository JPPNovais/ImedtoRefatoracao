namespace Imedto.Backend.Contracts.Admin.Dashboard.Queries;

/// <summary>
/// Query com filtros opcionais para o feed de audit log.
/// Período: "hoje" | "7d" | "30d" | "90d" | "todos". Default: "7d".
/// </summary>
public class ListarAuditLogDashboardQuery
{
    public string? Acao { get; set; }
    public Guid? AdminId { get; set; }
    public string Periodo { get; set; } = "7d";
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}
