namespace Imedto.Backend.Contracts.Admin.ConfigTrial.Commands;

public record AtualizarConfigTrialAdminCommand(
    Guid PlanoTrialId,
    int DuracaoTrialDias,
    bool TrialHabilitado,
    string Motivo,
    Guid AdminId);
