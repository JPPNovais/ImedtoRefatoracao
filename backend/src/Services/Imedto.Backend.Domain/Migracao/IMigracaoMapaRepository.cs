namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Repositório de <see cref="MigracaoMapa"/>.
/// Falha-fechada: toda query filtra por estabelecimentoId (CA2, multi-tenant).
/// </summary>
public interface IMigracaoMapaRepository
{
    Task Salvar(MigracaoMapa mapa, CancellationToken ct = default);

    Task<List<MigracaoMapa>> ListarPorJob(long jobId, long estabelecimentoId, CancellationToken ct = default);

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
}
