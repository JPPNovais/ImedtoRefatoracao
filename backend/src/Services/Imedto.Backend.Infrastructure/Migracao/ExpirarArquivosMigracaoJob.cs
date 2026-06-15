using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Domain.Migracao;
using Microsoft.Extensions.Logging;

namespace Imedto.Backend.Infrastructure.Migracao;

/// <summary>
/// Job recorrente que expira arquivos ZIP de migração após 30 dias (CA24, R12).
///
/// Fluxo por job elegível:
/// 1. Remove o arquivo do S3 via <see cref="IMigracaoArquivoStorageService"/>.
/// 2. Marca <see cref="MigracaoJob.ArquivoExpirado"/> = true.
/// 3. Persiste — o staging do job permanece auditável sem o bruto (CA24).
///
/// Nome: "expirar-arquivos-migracao" — cadastrado em <see cref="JobsRegistrados"/>.
/// Roda 1×/dia. Falha em job individual não derruba o processo inteiro.
/// </summary>
public sealed class ExpirarArquivosMigracaoJob : IJobHandler
{
    public string Nome => "expirar-arquivos-migracao";

    private readonly IMigracaoJobRepository _repo;
    private readonly IMigracaoArquivoStorageService _storage;
    private readonly ILogger<ExpirarArquivosMigracaoJob> _logger;

    public ExpirarArquivosMigracaoJob(
        IMigracaoJobRepository repo,
        IMigracaoArquivoStorageService storage,
        ILogger<ExpirarArquivosMigracaoJob> logger)
    {
        _repo = repo;
        _storage = storage;
        _logger = logger;
    }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        var elegíveis = await _repo.ListarComArquivoParaExpirar(agora, ct);

        if (elegíveis.Count == 0)
        {
            _logger.LogDebug("[Job:{Nome}] Nenhum arquivo de migração para expirar.", Nome);
            return;
        }

        _logger.LogInformation(
            "[Job:{Nome}] {Quantidade} arquivo(s) de migração para expirar.",
            Nome, elegíveis.Count);

        var expirados = 0;
        foreach (var job in elegíveis)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                // Remove do S3 — idempotente (S3 retorna 204 mesmo se a key não existir).
                await _storage.RemoverArquivoAsync(job.ArquivoS3Key!, ct);

                job.MarcarArquivoExpirado();
                await _repo.Salvar(job, ct);
                expirados++;
            }
            catch (Exception ex)
            {
                // Falha individual não derruba os outros — log sem PII.
                _logger.LogError(ex,
                    "[Job:{Nome}] Falha ao expirar arquivo do job {JobId}.", Nome, job.Id);
            }
        }

        _logger.LogInformation(
            "[Job:{Nome}] {Expirados}/{Total} arquivo(s) expirado(s).", Nome, expirados, elegíveis.Count);
    }
}
