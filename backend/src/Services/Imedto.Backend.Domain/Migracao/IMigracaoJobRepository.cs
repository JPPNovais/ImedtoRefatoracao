namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Repositório de escrita para <see cref="MigracaoJob"/>.
/// Falha-fechada: ausência de tenant → lança, nunca retorna dados de outro tenant.
/// </summary>
public interface IMigracaoJobRepository
{
    Task Salvar(MigracaoJob job, CancellationToken ct = default);

    Task<MigracaoJob?> ObterPorIdDoEstabelecimentoOuNulo(long jobId, long estabelecimentoId, CancellationToken ct = default);

    /// <summary>Lista jobs cujo arquivo está expirado e ainda não foi marcado como expirado.</summary>
    Task<List<MigracaoJob>> ListarComArquivoParaExpirar(DateTime corte, CancellationToken ct = default);

    /// <summary>
    /// Retorna o job mais antigo com status "aguardando_mapa" para o job de inferência processar.
    /// Null se não houver nenhum.
    /// </summary>
    Task<MigracaoJob?> ObterMaisAntigoAguardandoMapaOuNulo(CancellationToken ct = default);

    /// <summary>
    /// Busca job por ID sem filtro de tenant — uso exclusivo do admin.
    /// </summary>
    Task<MigracaoJob?> ObterPorIdAdminOuNulo(long jobId, CancellationToken ct = default);

    /// <summary>
    /// Lista todos os jobs (admin) — sem filtro de tenant para visão administrativa.
    /// </summary>
    Task<(List<MigracaoJob> Itens, int Total)> ListarAdmin(
        long? estabelecimentoId,
        string? status,
        int pagina,
        int tamanho,
        CancellationToken ct = default);

    /// <summary>Retorna o job mais antigo com status "migrando". Null se não houver.</summary>
    Task<MigracaoJob?> ObterMaisAntigoMigrandoOuNulo(CancellationToken ct = default);

    /// <summary>
    /// Retorna o job mais antigo com status "migrando" e onda="prontuario" (Onda 2 — CA13).
    /// Null se não houver.
    /// </summary>
    Task<MigracaoJob?> ObterMaisAntigoMigrandoOnda2OuNulo(CancellationToken ct = default);

    /// <summary>
    /// CA13 — verifica se o tenant tem algum job de Onda 1 (pacientes) ainda ativo
    /// (status: migrando, preview_pronto, mapa_em_revisao, aguardando_mapa).
    /// Retorna true se existir — a Onda 2 deve ficar bloqueada.
    /// </summary>
    Task<bool> ExisteOnda1AtivaParaTenant(long estabelecimentoId, CancellationToken ct = default);
}
