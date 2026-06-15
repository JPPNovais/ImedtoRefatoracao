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
}
