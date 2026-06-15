namespace Imedto.Backend.Contracts.Admin.Migracao;

public class RelatorioMigracaoResult
{
    public int TotalCriados { get; set; }
    public int TotalAtualizados { get; set; }
    public int TotalRejeitados { get; set; }
    public int TotalPulados { get; set; }
    public Dictionary<string, RelatorioEntidadeResult> PorEntidade { get; set; } = new();
}

public class RelatorioEntidadeResult
{
    public int Criados { get; set; }
    public int Atualizados { get; set; }
    public int Rejeitados { get; set; }
    public int Pulados { get; set; }
    public List<string> MotivosRejeicao { get; set; } = new();
}
