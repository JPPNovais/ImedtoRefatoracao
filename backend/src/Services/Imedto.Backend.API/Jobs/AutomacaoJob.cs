using Imedto.Backend.Application.Automacoes.Commands;
using Imedto.Backend.Contracts.Automacoes.Commands;

namespace Imedto.Backend.API.Jobs;

public class AutomacaoJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomacaoJob> _logger;

    public AutomacaoJob(IServiceScopeFactory scopeFactory, ILogger<AutomacaoJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Aguarda 30s no boot para o app subir completamente antes da primeira execução.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ExecutarJobs(stoppingToken);
        }
    }

    public async Task ExecutarJobs(CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        await ExecutarComLog(
            "ExpirarOrcamentosVencidos",
            () => sp.GetRequiredService<ExpirarOrcamentosVencidosCommandHandler>()
                    .Handle(new ExpirarOrcamentosVencidosCommand()),
            ct);

        await ExecutarComLog(
            "EnviarLembretesAgendamentos",
            () => sp.GetRequiredService<EnviarLembretesAgendamentosCommandHandler>()
                    .Handle(new EnviarLembretesAgendamentosCommand()),
            ct);
    }

    private async Task ExecutarComLog(string nome, Func<Task> job, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("[AutomacaoJob] Iniciando {Job}", nome);
            await job();
            _logger.LogInformation("[AutomacaoJob] Concluído {Job}", nome);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[AutomacaoJob] Erro em {Job}", nome);
        }
    }
}
