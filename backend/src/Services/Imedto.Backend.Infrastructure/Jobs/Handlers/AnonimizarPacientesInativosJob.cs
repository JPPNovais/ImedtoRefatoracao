using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Domain.Lgpd;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Jobs.Handlers;

/// <summary>
/// Job mensal de retenção LGPD (CFM 1.821/07 — prazo de guarda de 20 anos).
///
/// Critério de elegibilidade:
///   - <c>deletado_em IS NOT NULL</c> (paciente já foi removido logicamente)
///   - <c>deletado_em &lt; NOW() - INTERVAL '20 years'</c>
///   - <c>anonimizado_em IS NULL</c> (ainda não anonimizado)
///
/// Não loga PII (nome, CPF, e-mail, telefone). Apenas o id e o total processado.
/// </summary>
public class AnonimizarPacientesInativosJob : IJobHandler
{
    public string Nome => "anonimizar-pacientes-inativos";

    private static readonly TimeSpan RetencaoLegal = TimeSpan.FromDays(365 * 20);

    private readonly AppDbContext _db;
    private readonly IAnonimizacaoService _anonimizacao;
    private readonly ILogger<AnonimizarPacientesInativosJob> _logger;

    public AnonimizarPacientesInativosJob(
        AppDbContext db,
        IAnonimizacaoService anonimizacao,
        ILogger<AnonimizarPacientesInativosJob> logger)
    {
        _db = db;
        _anonimizacao = anonimizacao;
        _logger = logger;
    }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        var corte = DateTime.UtcNow - RetencaoLegal;

        // SQL equivalente:
        //   SELECT id FROM pacientes
        //   WHERE deletado_em IS NOT NULL
        //     AND deletado_em < NOW() - INTERVAL '20 years'
        //     AND anonimizado_em IS NULL
        var elegiveisIds = await _db.Pacientes
            .AsNoTracking()
            .Where(p =>
                p.DeletadoEm != null &&
                p.DeletadoEm < corte &&
                p.AnonimizadoEm == null)
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (elegiveisIds.Count == 0)
        {
            _logger.LogInformation("[Job:{Nome}] Nenhum paciente elegível para anonimização.", Nome);
            return;
        }

        _logger.LogInformation(
            "[Job:{Nome}] {Total} paciente(s) elegíveis para anonimização por retenção vencida.",
            Nome, elegiveisIds.Count);

        var processados = 0;
        foreach (var id in elegiveisIds)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // executadoPor = null → job automático; coluna fica null na tabela.
                await _anonimizacao.AnonimizarPaciente(id, MotivoAnonimizacao.RetencaoVencida, executadoPor: null, ct);
                processados++;
            }
            catch (Exception ex)
            {
                // Um erro em um paciente não deve parar o job inteiro.
                _logger.LogError(ex,
                    "[Job:{Nome}] Falha ao anonimizar paciente id={Id}.", Nome, id);
            }
        }

        _logger.LogInformation(
            "[Job:{Nome}] Anonimização concluída: {Processados}/{Total} paciente(s) processados.",
            Nome, processados, elegiveisIds.Count);
    }
}
