using Imedto.Backend.Domain.AssinaturaDigital;
using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Domain.Receitas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Imedto.Backend.Infrastructure.AssinaturaDigital;

/// <summary>
/// Job recorrente (1×/hora) que expira receitas em AssinaturaPendente sem resposta.
/// O limite de minutos é configurável via <c>AssinaturaDigital:ExpiracaoPendenteMinutos</c>.
/// Implementa a lógica diretamente (sem Application handler) para evitar dependência circular
/// Infrastructure→Application. Segue o padrão de <see cref="Imedto.Backend.Infrastructure.Assinaturas.Handlers.ExpirarTrialsJob"/>.
/// </summary>
public class ExpirarAssinaturasPendentesJob : IJobHandler
{
    public string Nome => "expirar-assinaturas-pendentes";

    private readonly IReceitaRepository _receitaRepo;
    private readonly IAssinaturaAuditLogRepository _auditRepo;
    private readonly IConfiguration _config;
    private readonly ILogger<ExpirarAssinaturasPendentesJob> _logger;

    public ExpirarAssinaturasPendentesJob(
        IReceitaRepository receitaRepo,
        IAssinaturaAuditLogRepository auditRepo,
        IConfiguration config,
        ILogger<ExpirarAssinaturasPendentesJob> logger)
    {
        _receitaRepo = receitaRepo;
        _auditRepo = auditRepo;
        _config = config;
        _logger = logger;
    }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        var limiteMinutos = _config.GetValue("AssinaturaDigital:ExpiracaoPendenteMinutos", 30);
        var corte = DateTime.UtcNow.AddMinutes(-Math.Abs(limiteMinutos));

        var pendentes = await _receitaRepo.ListarPendentesParaExpirarAsync(corte, ct);

        if (pendentes.Count == 0)
        {
            _logger.LogDebug("[Job:{Nome}] Nenhuma receita elegível para expiração.", Nome);
            return;
        }

        _logger.LogInformation(
            "[Job:{Nome}] {Quantidade} receita(s) pendentes elegíveis.",
            Nome, pendentes.Count);

        var expiradas = 0;
        foreach (var receita in pendentes)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                receita.ExpirarAssinaturaPendente();
                await _receitaRepo.Salvar(receita);

                var audit = AssinaturaAuditLog.Registrar(
                    receitaId: receita.Id,
                    estabelecimentoId: receita.EstabelecimentoId,
                    usuarioId: Guid.Empty, // sistema.
                    acao: "EXPIRAR_PENDENTE",
                    statusAnterior: StatusAssinaturaDigital.AssinaturaPendente.ToString(),
                    statusNovo: receita.AssinaturaDigitalStatus.ToString());
                await _auditRepo.SalvarAsync(audit, ct);

                expiradas++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Job:{Nome}] Falha ao expirar receita {ReceitaId}.", Nome, receita.Id);
            }
        }

        _logger.LogInformation(
            "[Job:{Nome}] {Expiradas}/{Total} receita(s) expiradas.", Nome, expiradas, pendentes.Count);
    }
}
