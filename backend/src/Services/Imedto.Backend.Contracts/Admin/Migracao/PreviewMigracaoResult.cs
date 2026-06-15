namespace Imedto.Backend.Contracts.Admin.Migracao;

public class PreviewMigracaoResult
{
    public int TotalRegistros { get; set; }
    public Dictionary<string, EntidadePreview> PorEntidade { get; set; } = new();
}

public class EntidadePreview
{
    public int Pendentes { get; set; }
}
