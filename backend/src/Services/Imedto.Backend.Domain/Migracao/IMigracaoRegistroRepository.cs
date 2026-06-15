namespace Imedto.Backend.Domain.Migracao;

public interface IMigracaoRegistroRepository
{
    Task SalvarLote(IReadOnlyList<MigracaoRegistro> registros, CancellationToken ct = default);
    Task Salvar(MigracaoRegistro registro, CancellationToken ct = default);
    Task<List<MigracaoRegistro>> ListarPorJob(long jobId, CancellationToken ct = default);
    Task<RelatorioMigracao> ObterRelatorio(long jobId, CancellationToken ct = default);
}

public class RelatorioMigracao
{
    public int TotalCriados { get; set; }
    public int TotalAtualizados { get; set; }
    public int TotalRejeitados { get; set; }
    public int TotalPulados { get; set; }
    public Dictionary<string, RelatorioEntidade> PorEntidade { get; set; } = new();
}

public class RelatorioEntidade
{
    public int Criados { get; set; }
    public int Atualizados { get; set; }
    public int Rejeitados { get; set; }
    public int Pulados { get; set; }
    public List<string> MotivosRejeicao { get; set; } = new();
}
