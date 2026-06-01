using Imedto.Backend.Contracts.AssinaturaDigital.Commands;
using Imedto.Backend.Domain.AssinaturaDigital;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Cqrs;
using Microsoft.Extensions.Logging;

namespace Imedto.Backend.Application.AssinaturaDigital.Commands;

/// <summary>
/// Expira receitas em AssinaturaPendente com mais de <c>LimiteMinutos</c> minutos.
/// Idempotente via <see cref="Domain.Receitas.Receita.ExpirarAssinaturaPendente"/> (ignora
/// se já resolvida). Invocado pelo job <c>expirar-assinaturas-pendentes</c> (1×/hora).
/// </summary>
public class ExpirarAssinaturasPendentesCommandHandler : ICommandHandler<ExpirarAssinaturasPendentesCommand>
{
    private readonly IReceitaRepository _receitaRepo;
    private readonly IAssinaturaAuditLogRepository _auditRepo;
    private readonly ILogger<ExpirarAssinaturasPendentesCommandHandler> _logger;

    public ExpirarAssinaturasPendentesCommandHandler(
        IReceitaRepository receitaRepo,
        IAssinaturaAuditLogRepository auditRepo,
        ILogger<ExpirarAssinaturasPendentesCommandHandler> logger)
    {
        _receitaRepo = receitaRepo;
        _auditRepo = auditRepo;
        _logger = logger;
    }

    public async Task Handle(ExpirarAssinaturasPendentesCommand cmd)
    {
        var limite = DateTime.UtcNow.AddMinutes(-Math.Abs(cmd.LimiteMinutos));
        var pendentes = await _receitaRepo.ListarPendentesParaExpirarAsync(limite);

        if (pendentes.Count == 0)
        {
            _logger.LogDebug("[Job:expirar-assinaturas-pendentes] Nenhuma receita elegível.");
            return;
        }

        _logger.LogInformation(
            "[Job:expirar-assinaturas-pendentes] {Quantidade} receita(s) pendentes elegíveis.",
            pendentes.Count);

        var expiradas = 0;
        foreach (var receita in pendentes)
        {
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
                await _auditRepo.SalvarAsync(audit);

                expiradas++;
            }
            catch (Exception ex)
            {
                // Falha em uma receita não interrompe as demais — idempotência garante re-tentativa.
                _logger.LogError(ex,
                    "[Job:expirar-assinaturas-pendentes] Falha ao expirar receita {ReceitaId}.",
                    receita.Id);
            }
        }

        _logger.LogInformation(
            "[Job:expirar-assinaturas-pendentes] {Expiradas}/{Total} receita(s) expiradas.",
            expiradas, pendentes.Count);
    }
}
