namespace Imedto.Backend.Domain.Migracao;

public interface IMigracaoJobEventoRepository
{
    Task Gravar(MigracaoJobEvento evento, CancellationToken ct = default);
    Task<List<MigracaoJobEvento>> ListarPorJob(long jobId, CancellationToken ct = default);
}
