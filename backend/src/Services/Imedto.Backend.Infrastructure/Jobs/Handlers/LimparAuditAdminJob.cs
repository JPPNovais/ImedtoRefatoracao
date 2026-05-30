using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Jobs.Handlers;

/// <summary>
/// Limpa linhas de <c>imedto_admin_audit_log</c> que ultrapassaram o TTL definido
/// em <see cref="AuditLogRetencao"/>. Roda 1×/dia (86400s).
///
/// Batches de 10.000 linhas por ação para evitar lock pesado em produção.
/// Ações não mapeadas no dicionário são limpas com o default de 365 dias.
/// Não registra audit do próprio job — zero auto-referência.
/// </summary>
public class LimparAuditAdminJob : IJobHandler
{
    public string Nome => "limpar-audit-admin";

    private const int BatchSize = 10_000;

    private readonly AppDbContext _db;
    private readonly ILogger<LimparAuditAdminJob> _logger;

    public LimparAuditAdminJob(AppDbContext db, ILogger<LimparAuditAdminJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        var agora = DateTimeOffset.UtcNow;
        var totalRodada = 0;

        // 1. Ações mapeadas explicitamente.
        foreach (var (acao, dias) in AuditLogRetencao.PorAcao)
        {
            var corte = agora.AddDays(-dias).UtcDateTime;
            var removidosDaAcao = 0;
            int batch;

            do
            {
                batch = await _db.ImedtoAdminAuditLogs
                    .Where(l => l.Acao == acao && l.CriadoEm < corte)
                    .Take(BatchSize)
                    .ExecuteDeleteAsync(ct);

                removidosDaAcao += batch;
            } while (batch == BatchSize && !ct.IsCancellationRequested);

            if (removidosDaAcao > 0)
                _logger.LogInformation(
                    "[Job:{Nome}] Ação {Acao}: removidas {Total} linhas (TTL {Dias}d).",
                    Nome, acao, removidosDaAcao, dias);

            totalRodada += removidosDaAcao;
        }

        // 2. Ações não mapeadas — aplica o default de 365 dias.
        var acoesConhecidas = AuditLogRetencao.PorAcao.Keys.ToList();
        var corteDefault = agora.AddDays(-AuditLogRetencao.DefaultDias).UtcDateTime;
        var removidosDefault = 0;
        int batchDefault;

        do
        {
            batchDefault = await _db.ImedtoAdminAuditLogs
                .Where(l => !acoesConhecidas.Contains(l.Acao) && l.CriadoEm < corteDefault)
                .Take(BatchSize)
                .ExecuteDeleteAsync(ct);

            removidosDefault += batchDefault;
        } while (batchDefault == BatchSize && !ct.IsCancellationRequested);

        if (removidosDefault > 0)
            _logger.LogInformation(
                "[Job:{Nome}] Ações não mapeadas (default {DefaultDias}d): removidas {Total} linhas.",
                Nome, AuditLogRetencao.DefaultDias, removidosDefault);

        totalRodada += removidosDefault;

        _logger.LogInformation(
            "[Job:{Nome}] Rodada concluída — total removido: {Total} linhas.",
            Nome, totalRodada);
    }
}
