using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Jobs.Handlers;

/// <summary>
/// Job recorrente (1×/dia, 03:00 BRT / 06:00 UTC) que expira agendamentos que
/// permaneceram em Agendado ou Confirmado sem receber baixa manual em D-1.
///
/// Varredura cross-tenant (global — sem filtrar por estabelecimento_id).
/// A janela é D-1 calculada no fuso America/Sao_Paulo para evitar que horários
/// das 21h-23h59 BRT (= dia seguinte em UTC) escapem da varredura (CA4).
///
/// Processamento em lote (~200) chamando o método de domínio por item — proibido
/// UPDATE em massa (R6 briefing 2026-06-19_001).
///
/// Log agregado por estabelecimento {estabelecimento_id, quantidade, timestamp}
/// sem PII individual (R5/CA9).
/// </summary>
public class ExpirarAgendamentosNaoFinalizadosJob : IJobHandler
{
    public string Nome => "expirar-agendamentos-nao-finalizados";

    private const int TamanhoBatch = 200;
    private const string MotivoExpiracao = "Expirado automaticamente — não finalizado até o fim do dia";

    private static readonly TimeZoneInfo FusoBrasilia =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    private readonly AppDbContext _db;
    private readonly ILogger<ExpirarAgendamentosNaoFinalizadosJob> _logger;

    public ExpirarAgendamentosNaoFinalizadosJob(
        AppDbContext db,
        ILogger<ExpirarAgendamentosNaoFinalizadosJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        // Fronteira D-1 em BRT → ontem 00:00 BRT (inclusivo) até hoje 00:00 BRT (exclusivo).
        // Convertido para UTC: inicio_previsto (timestamptz) é comparado em UTC.
        var agora = DateTime.UtcNow;
        var hojeBrt = TimeZoneInfo.ConvertTimeFromUtc(agora, FusoBrasilia).Date;
        var ontemBrt = hojeBrt.AddDays(-1);

        var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(ontemBrt, DateTimeKind.Unspecified), FusoBrasilia);
        var fimUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(hojeBrt, DateTimeKind.Unspecified), FusoBrasilia);

        _logger.LogInformation(
            "[Job:{Nome}] Iniciando varredura D-1: janela UTC [{Inicio:o}, {Fim:o}).",
            Nome, inicioUtc, fimUtc);

        // Coleta IDs elegíveis sem tracking (evita carregar objetos grandes desnecessariamente).
        // O índice parcial (inicio_previsto) WHERE status IN ('Agendado','Confirmado')
        // garante que esta query não faça seq scan na tabela inteira.
        var ids = await _db.Agendamentos
            .AsNoTracking()
            .Where(a =>
                a.InicioPrevisto >= inicioUtc &&
                a.InicioPrevisto < fimUtc &&
                (a.Status == AgendamentoStatus.Agendado || a.Status == AgendamentoStatus.Confirmado))
            .Select(a => a.Id)
            .ToListAsync(ct);

        if (ids.Count == 0)
        {
            _logger.LogInformation("[Job:{Nome}] Nenhum agendamento elegível em D-1.", Nome);
            return;
        }

        _logger.LogInformation(
            "[Job:{Nome}] {Total} agendamento(s) elegíveis para expiração.", Nome, ids.Count);

        // Processamento em lote — respeita R6 (método de domínio por item, nunca UPDATE em massa).
        // Log agregado por estabelecimento ao fim — sem PII (CA9).
        var contadorPorEstabelecimento = new Dictionary<long, int>();
        var erros = 0;

        for (var offset = 0; offset < ids.Count; offset += TamanhoBatch)
        {
            if (ct.IsCancellationRequested) break;

            var lote = ids.Skip(offset).Take(TamanhoBatch).ToList();

            // Carrega o lote com tracking para poder salvar.
            var agendamentos = await _db.Agendamentos
                .Where(a => lote.Contains(a.Id))
                .ToListAsync(ct);

            foreach (var ag in agendamentos)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    ag.ExpirarPorFimDoDia(MotivoExpiracao);
                    contadorPorEstabelecimento.TryGetValue(ag.EstabelecimentoId, out var atual);
                    contadorPorEstabelecimento[ag.EstabelecimentoId] = atual + 1;
                }
                catch (Exception ex)
                {
                    erros++;
                    // Um erro em um item não deve parar o lote inteiro.
                    _logger.LogError(ex,
                        "[Job:{Nome}] Falha ao expirar agendamento id={Id}.", Nome, ag.Id);
                }
            }

            // Persiste o lote de uma vez — um SaveChanges por batch, não por item.
            await _db.SaveChangesAsync(ct);
        }

        // Log agregado por estabelecimento (CA9 — sem PII).
        foreach (var (estabelecimentoId, quantidade) in contadorPorEstabelecimento)
        {
            _logger.LogInformation(
                "[Job:{Nome}] Estabelecimento {EstabelecimentoId}: {Quantidade} agendamento(s) expirado(s) em {Timestamp:o}.",
                Nome, estabelecimentoId, quantidade, agora);
        }

        _logger.LogInformation(
            "[Job:{Nome}] Concluído: {Total} expirado(s), {Erros} erro(s).",
            Nome, ids.Count - erros, erros);
    }
}
