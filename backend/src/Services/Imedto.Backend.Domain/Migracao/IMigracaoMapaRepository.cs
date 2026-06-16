namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Repositório de <see cref="MigracaoMapa"/>.
/// Falha-fechada: toda query filtra por estabelecimentoId (CA2, multi-tenant).
/// Addendum 4: busca por bloco usa (jobId, entidade, nomeBlocoOrigem) como chave de upsert.
/// </summary>
public interface IMigracaoMapaRepository
{
    Task Salvar(MigracaoMapa mapa, CancellationToken ct = default);

    Task<List<MigracaoMapa>> ListarPorJob(long jobId, long estabelecimentoId, CancellationToken ct = default);

    /// <summary>
    /// Addendum 4: busca por (jobId, entidade, nomeBlocoOrigem).
    /// Para CSV/JSON-array, nomeBlocoOrigem é string.Empty (compatibilidade).
    /// </summary>
    Task<MigracaoMapa?> ObterPorJobEntidadeBlocoOuNulo(
        long jobId,
        string entidade,
        string nomeBlocoOrigem,
        long estabelecimentoId,
        CancellationToken ct = default);

    /// <summary>
    /// Compatibilidade: busca pelo contrato antigo (entidade sem bloco).
    /// Internamente usa nomeBlocoOrigem = string.Empty.
    /// </summary>
    Task<MigracaoMapa?> ObterPorJobEEntidadeOuNulo(
        long jobId,
        string entidade,
        long estabelecimentoId,
        CancellationToken ct = default);

    /// <summary>
    /// Busca sem filtro de tenant — uso exclusivo do admin.
    /// </summary>
    Task<MigracaoMapa?> ObterPorJobEEntidadeAdminOuNulo(
        long jobId,
        string entidade,
        CancellationToken ct = default);

    /// <summary>
    /// Addendum 4: busca por (jobId, entidade, nomeBlocoOrigem) sem filtro de tenant.
    /// Uso exclusivo do contexto admin (sem estabelecimentoId disponível).
    /// Para CSV/JSON-array (nomeBlocoOrigem = ""), delega ao método legado.
    /// </summary>
    Task<MigracaoMapa?> ObterPorJobEntidadeBlocoAdminOuNulo(
        long jobId,
        string entidade,
        string nomeBlocoOrigem,
        CancellationToken ct = default);

    /// <summary>
    /// Addendum 5 — reprocessar parcial (CA97/R-R8):
    /// Busca mapa apenas por (jobId, nomeBlocoOrigem, estabelecimentoId), sem filtrar por entidade.
    /// Necessário porque ao reprocessar não sabemos a entidade prévia do bloco com erro.
    /// </summary>
    Task<MigracaoMapa?> ObterPorJobBlocoOuNulo(
        long jobId,
        string nomeBlocoOrigem,
        long estabelecimentoId,
        CancellationToken ct = default);
}
