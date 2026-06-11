namespace Imedto.Backend.Contracts.Admin.ConfigTrial.Queries.Results;

public class ConfigTrialAdminDto
{
    public Guid Id { get; set; }
    public Guid PlanoTrialId { get; set; }
    public string PlanoTrialNome { get; set; } = string.Empty;
    public int DuracaoTrialDias { get; set; }
    public bool TrialHabilitado { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; }
}
