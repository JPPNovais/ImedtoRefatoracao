using Imedto.Backend.Contracts.Admin.ConfigTrial.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.ConfigTrial;

/// <summary>
/// Atualiza a configuração global de trial automático.
/// Resolve CA30.
/// </summary>
public class AtualizarConfigTrialAdminCommandHandler
{
    private readonly IImedtoConfigTrialRepository _configRepo;
    private readonly IImedtoPlanoRepository _planoRepo;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public AtualizarConfigTrialAdminCommandHandler(
        IImedtoConfigTrialRepository configRepo,
        IImedtoPlanoRepository planoRepo,
        ImedtoAdminAuditWriter audit,
        AppDbContext db)
    {
        _configRepo = configRepo;
        _planoRepo = planoRepo;
        _audit = audit;
        _db = db;
    }

    public async Task Handle(AtualizarConfigTrialAdminCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Motivo))
            throw new BusinessException("Motivo é obrigatório.");

        var plano = await _planoRepo.ObterPorIdAsync(cmd.PlanoTrialId, ct)
            ?? throw new BusinessException("Plano de trial não encontrado.");

        if (!plano.Ativo)
            throw new BusinessException("Não é possível definir um plano inativo como plano de trial.");

        var config = await _configRepo.ObterAsync(ct);
        if (config is null)
        {
            // Cria o singleton se por algum motivo não existir (seed já faz isso normalmente)
            config = ImedtoConfigTrial.CriarPadrao(cmd.PlanoTrialId);
            config.Atualizar(cmd.PlanoTrialId, cmd.DuracaoTrialDias, cmd.TrialHabilitado, cmd.AdminId);
            _configRepo.Adicionar(config);
        }
        else
        {
            config.Atualizar(cmd.PlanoTrialId, cmd.DuracaoTrialDias, cmd.TrialHabilitado, cmd.AdminId);
            _configRepo.Atualizar(config);
        }

        await _db.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.AtualizarConfig,
            cmd.AdminId,
            recursoTipo: "config_trial",
            recursoId: config.Id.ToString(),
            motivo: cmd.Motivo,
            payloadJson: $"{{\"plano_trial\":\"{plano.Nome}\",\"duracao_dias\":{cmd.DuracaoTrialDias},\"habilitado\":{cmd.TrialHabilitado.ToString().ToLower()}}}",
            ct: ct);
    }
}
