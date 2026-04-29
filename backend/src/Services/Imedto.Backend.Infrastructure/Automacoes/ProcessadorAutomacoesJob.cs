using System.Text.Json;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Jobs;

namespace Imedto.Backend.Infrastructure.Automacoes;

/// <summary>
/// Worker registrado no scheduler (item 2.1) que drena a fila <c>automation_events</c>.
/// Roda a cada 30s — single-leader garantido pelo advisory lock do <c>JobScheduler</c>.
///
/// Loop por evento:
/// <list type="number">
/// <item>Marcar Executando + persistir (reserva).</item>
/// <item>Carregar regra → executor processa <c>acoes_json</c>.</item>
/// <item>Sucesso → MarcarConcluido. Falha → MarcarFalhou (backoff até 3 tentativas).</item>
/// </list>
/// Falhas individuais nunca derrubam o lote — cada evento é isolado em try/catch.
/// </summary>
public class ProcessadorAutomacoesJob : IJobHandler
{
    public string Nome => "processar-automacoes";

    private readonly IEventoAutomacaoRepository _eventoRepo;
    private readonly IRegraAutomacaoRepository _regraRepo;
    private readonly IExecutorAcao _executor;
    private readonly ILogger<ProcessadorAutomacoesJob> _logger;

    public ProcessadorAutomacoesJob(
        IEventoAutomacaoRepository eventoRepo,
        IRegraAutomacaoRepository regraRepo,
        IExecutorAcao executor,
        ILogger<ProcessadorAutomacoesJob> logger)
    {
        _eventoRepo = eventoRepo;
        _regraRepo = regraRepo;
        _executor = executor;
        _logger = logger;
    }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        var pendentes = await _eventoRepo.ListarPendentesProntos(DateTime.UtcNow);
        if (pendentes.Count == 0) return;

        _logger.LogInformation(
            "[Job:{Nome}] Processando {Quantidade} evento(s) de automação.",
            Nome, pendentes.Count);

        foreach (var evento in pendentes)
        {
            ct.ThrowIfCancellationRequested();
            await ProcessarEventoAsync(evento, ct);
        }
    }

    private async Task ProcessarEventoAsync(EventoAutomacao evento, CancellationToken ct)
    {
        // Reserva (state machine: Pendente → Executando)
        try
        {
            evento.MarcarExecutando();
            await _eventoRepo.Salvar(evento);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Job:{Nome}] Falha ao reservar evento {Id} — pulando.", Nome, evento.Id);
            return;
        }

        // Resolve regra + executa ações
        try
        {
            var regra = await _regraRepo.ObterPorIdOuNulo(evento.RegraId);
            if (regra is null)
                throw new InvalidOperationException($"Regra {evento.RegraId} não existe — evento órfão.");
            if (!regra.Ativa)
                throw new InvalidOperationException($"Regra {evento.RegraId} foi desativada após enfileirar — abortando ação.");

            using var payload = JsonDocument.Parse(evento.PayloadJson);
            await _executor.ExecutarAsync(regra.AcoesJson, payload, regra.EstabelecimentoId, ct);

            evento.MarcarConcluido();
            await _eventoRepo.Salvar(evento);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("[Job:{Nome}] Evento {Id} interrompido por shutdown.", Nome, evento.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[Job:{Nome}] Evento {Id} (regra {RegraId}) falhou — tentativa {Tentativa}.",
                Nome, evento.Id, evento.RegraId, evento.TentativaN + 1);

            try
            {
                evento.MarcarFalhou(ex.Message);
                await _eventoRepo.Salvar(evento);
            }
            catch (Exception persistEx)
            {
                _logger.LogError(persistEx,
                    "[Job:{Nome}] Falha ao persistir falha do evento {Id}.", Nome, evento.Id);
            }
        }
    }
}
