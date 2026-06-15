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

    /// <summary>
    /// Addendum 002 — D-C1/CA34: agregação motivo → quantidade para rejeitados.
    /// Categorias genéricas sem PII (ex.: "CPF ausente": 12).
    /// </summary>
    public Dictionary<string, int> MotivosRejeicao { get; set; } = new();

    /// <summary>
    /// Addendum 002 — D-C2/CA35: agregação motivo → quantidade para pulados.
    /// Ex.: "identificador ausente": 5.
    /// </summary>
    public Dictionary<string, int> MotivosPulo { get; set; } = new();
}
