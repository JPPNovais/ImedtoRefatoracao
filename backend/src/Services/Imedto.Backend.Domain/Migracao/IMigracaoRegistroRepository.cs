namespace Imedto.Backend.Domain.Migracao;

public interface IMigracaoRegistroRepository
{
    Task SalvarLote(IReadOnlyList<MigracaoRegistro> registros, CancellationToken ct = default);
    Task Salvar(MigracaoRegistro registro, CancellationToken ct = default);
    Task<List<MigracaoRegistro>> ListarPorJob(long jobId, CancellationToken ct = default);
    Task<RelatorioMigracao> ObterRelatorio(long jobId, CancellationToken ct = default);

    /// <summary>
    /// Retorna apenas os registros com status <c>importado_criado</c> do job.
    /// Usado pelo rollback (CA17) para identificar o que reverter.
    /// </summary>
    Task<List<MigracaoRegistro>> ListarCriadosPorJob(long jobId, CancellationToken ct = default);
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

    /// <summary>
    /// Addendum 002 — D-C1/CA34: motivo → quantidade para rejeitados.
    /// Chaves são categorias genéricas sem PII.
    /// </summary>
    public Dictionary<string, int> MotivosRejeicao { get; set; } = new();

    /// <summary>
    /// Addendum 002 — D-C2/CA35: motivo → quantidade para pulados.
    /// </summary>
    public Dictionary<string, int> MotivosPulo { get; set; } = new();
}
