using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Imedto.Backend.Domain.Jobs;

namespace Imedto.Backend.Infrastructure.Jobs;

/// <summary>
/// Scheduler nativo de jobs. Em multi-instância, apenas um processo por rodada
/// segura o <c>pg_try_advisory_lock</c> e processa a fila de <c>jobs_agendados</c>.
/// Os outros dormem e tentam de novo no próximo poll — se o líder cair, sua sessão
/// Postgres é encerrada e o lock é liberado automaticamente pelo servidor.
///
/// Características:
/// <list type="bullet">
/// <item>Lock advisory de sessão obtido em conexão Npgsql dedicada (não reutiliza o pool do EF).</item>
/// <item>Scope DI próprio por execução de job — cada handler ganha um <c>AppDbContext</c> fresco.</item>
/// <item>Falha em job nunca derruba o serviço; é registrada no aggregate via <see cref="JobAgendado.MarcarFalhou"/>.</item>
/// <item>Bootstrap: na primeira passada cria linhas faltantes para handlers de <see cref="JobsRegistrados"/>.</item>
/// </list>
/// </summary>
public class JobScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JobScheduler> _logger;
    private readonly TimeSpan _pollInterval;
    private readonly long _advisoryLockKey;

    public JobScheduler(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<JobScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;

        var pollSeconds = configuration.GetValue<int?>("Scheduler:PollIntervalSeconds") ?? 30;
        _pollInterval = TimeSpan.FromSeconds(Math.Max(5, pollSeconds));
        _advisoryLockKey = configuration.GetValue<long?>("Scheduler:AdvisoryLockKey") ?? 8475200001L;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Pequeno delay no boot para deixar o app subir antes da primeira passada.
        try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
        catch (OperationCanceledException) { return; }

        await SemearJobsRegistrados(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecutarRodada(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[JobScheduler] Erro inesperado na rodada — continuando.");
            }

            try { await Task.Delay(_pollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>
    /// Tenta obter liderança via advisory lock e, se conseguir, processa o lote de jobs prontos.
    /// O lock fica vivo só durante o processamento — liberado no <c>finally</c>.
    /// </summary>
    private async Task ExecutarRodada(CancellationToken ct)
    {
        var connectionString = _configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("[JobScheduler] ConnectionString:Default não configurada — pulando rodada.");
            return;
        }

        await using var conexao = new NpgsqlConnection(connectionString);
        await conexao.OpenAsync(ct);

        var obteveLock = await TentarAdvisoryLock(conexao, ct);
        if (!obteveLock)
        {
            _logger.LogDebug("[JobScheduler] Outra instância é a líder nesta rodada — dormindo.");
            return;
        }

        try
        {
            await ProcessarJobsProntos(ct);
        }
        finally
        {
            await LiberarAdvisoryLock(conexao, ct);
        }
    }

    private async Task ProcessarJobsProntos(CancellationToken ct)
    {
        // Scope só para a query inicial — cada job ganha seu próprio scope depois.
        List<JobAgendado> prontos;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IJobAgendadoRepository>();
            prontos = await repo.ListarProntosParaExecutar(DateTime.UtcNow);
        }

        if (prontos.Count == 0) return;

        _logger.LogInformation("[JobScheduler] {Quantidade} job(s) prontos para executar.", prontos.Count);

        foreach (var job in prontos)
        {
            if (ct.IsCancellationRequested) break;
            await ExecutarJob(job.Id, ct);
        }
    }

    /// <summary>
    /// Executa um único job em scope DI próprio. Recarrega o aggregate dentro do scope
    /// para evitar tracking cross-scope. Falhas são capturadas e gravadas no próprio aggregate.
    /// </summary>
    private async Task ExecutarJob(long jobId, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var repo = sp.GetRequiredService<IJobAgendadoRepository>();

        // Recarrega o aggregate no scope atual (DbContext novo) para evitar problemas de tracking.
        var jobs = await repo.ListarProntosParaExecutar(DateTime.UtcNow.AddSeconds(1));
        var job = jobs.FirstOrDefault(j => j.Id == jobId);
        if (job is null)
        {
            // Provavelmente outro nó pegou ou status mudou — apenas pula.
            return;
        }

        var handler = sp.GetServices<IJobHandler>().FirstOrDefault(h => h.Nome == job.Nome);
        if (handler is null)
        {
            _logger.LogWarning("[JobScheduler] Handler não registrado para job '{Nome}'.", job.Nome);
            try
            {
                job.MarcarExecutando();
                job.MarcarFalhou("Handler não encontrado para este job.");
                await repo.Salvar(job);
            }
            catch (Exception persistenciaEx)
            {
                _logger.LogError(persistenciaEx,
                    "[JobScheduler] Falha ao persistir estado de erro do job '{Nome}'.", job.Nome);
            }
            return;
        }

        try
        {
            job.MarcarExecutando();
            await repo.Salvar(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JobScheduler] Falha ao reservar job '{Nome}' — pulando.", job.Nome);
            return;
        }

        try
        {
            _logger.LogInformation("[JobScheduler] Iniciando job '{Nome}' (id {Id}).", job.Nome, job.Id);
            await handler.ExecutarAsync(ct);
            job.MarcarConcluido();
            await repo.Salvar(job);
            _logger.LogInformation("[JobScheduler] Job '{Nome}' concluído.", job.Nome);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutdown — não trata como falha. O job volta a ser pego na próxima rodada
            // após o scheduler subir de novo (status fica em Executando até timeout natural).
            _logger.LogInformation("[JobScheduler] Job '{Nome}' interrompido por shutdown.", job.Nome);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JobScheduler] Job '{Nome}' falhou.", job.Nome);
            try
            {
                job.MarcarFalhou(ex.Message);
                await repo.Salvar(job);
            }
            catch (Exception persistenciaEx)
            {
                _logger.LogError(persistenciaEx,
                    "[JobScheduler] Falha ao persistir falha do job '{Nome}'.", job.Nome);
            }
        }
    }

    /// <summary>
    /// Garante que cada job recorrente conhecido tem linha em <c>jobs_agendados</c>.
    /// Idempotente — apenas insere o que falta. Chamado uma vez no boot.
    /// </summary>
    private async Task SemearJobsRegistrados(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IJobAgendadoRepository>();

            foreach (var entry in JobsRegistrados.Todos)
            {
                if (ct.IsCancellationRequested) return;

                var existente = await repo.ObterPorNomeOuNulo(entry.Nome);
                if (existente is not null) continue;

                var job = JobAgendado.Agendar(
                    nome: entry.Nome,
                    primeiroRunEm: DateTime.UtcNow.AddSeconds(30),
                    intervaloSeg: entry.IntervaloSeg);
                await repo.Salvar(job);

                _logger.LogInformation(
                    "[JobScheduler] Job '{Nome}' registrado (intervalo {Intervalo}s).",
                    entry.Nome, entry.IntervaloSeg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JobScheduler] Falha no bootstrap de jobs registrados.");
        }
    }

    private async Task<bool> TentarAdvisoryLock(NpgsqlConnection conexao, CancellationToken ct)
    {
        await using var cmd = conexao.CreateCommand();
        cmd.CommandText = "SELECT pg_try_advisory_lock(@key);";
        cmd.Parameters.AddWithValue("key", _advisoryLockKey);
        var resultado = await cmd.ExecuteScalarAsync(ct);
        return resultado is bool b && b;
    }

    private async Task LiberarAdvisoryLock(NpgsqlConnection conexao, CancellationToken ct)
    {
        try
        {
            await using var cmd = conexao.CreateCommand();
            cmd.CommandText = "SELECT pg_advisory_unlock(@key);";
            cmd.Parameters.AddWithValue("key", _advisoryLockKey);
            await cmd.ExecuteScalarAsync(ct);
        }
        catch (Exception ex)
        {
            // Não-crítico: se a sessão cair, o Postgres libera o lock sozinho.
            _logger.LogWarning(ex, "[JobScheduler] Falha ao liberar advisory lock — será liberado no fechamento da sessão.");
        }
    }
}
