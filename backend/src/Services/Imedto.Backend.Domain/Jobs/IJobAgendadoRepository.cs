namespace Imedto.Backend.Domain.Jobs;

/// <summary>
/// Repositório de <see cref="JobAgendado"/>. Escrita e leitura via EF — a tabela é pequena
/// (≤ poucas dezenas de linhas), então não justifica Dapper.
/// </summary>
public interface IJobAgendadoRepository
{
    /// <summary>
    /// Lista jobs prontos para executar agora: <c>Status = Pendente</c> e <c>ProximoRunEm &lt;= agora</c>.
    /// Ordenado por <c>ProximoRunEm</c> para favorecer o que está mais atrasado.
    /// </summary>
    Task<List<JobAgendado>> ListarProntosParaExecutar(DateTime agora);

    /// <summary>Obtém um job pelo nome único (ou <c>null</c> se não existe).</summary>
    Task<JobAgendado?> ObterPorNomeOuNulo(string nome);

    /// <summary>Persiste o aggregate (insert se Id = 0, update caso contrário) e dá <c>SaveChanges</c>.</summary>
    Task Salvar(JobAgendado job);
}
