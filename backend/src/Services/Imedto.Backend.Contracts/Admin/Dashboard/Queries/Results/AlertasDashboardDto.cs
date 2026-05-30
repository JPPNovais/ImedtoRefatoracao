namespace Imedto.Backend.Contracts.Admin.Dashboard.Queries.Results;

/// <summary>Alertas acionáveis do dashboard. POCO class para Dapper.</summary>
public class AlertasDashboardDto
{
    public IReadOnlyList<TrialExpirandoDto> TrialsExpirando { get; set; } = [];
    public IReadOnlyList<SemAssinaturaDto> SemAssinatura { get; set; } = [];
    public int SemAssinaturaTotal { get; set; }
}

public class TrialExpirandoDto
{
    public long EstabelecimentoId { get; set; }
    public string NomeFantasia { get; set; } = string.Empty;
    public string DonoNome { get; set; } = string.Empty;
    public DateTimeOffset FimEm { get; set; }
    public int DiasRestantes { get; set; }
}

public class SemAssinaturaDto
{
    public long EstabelecimentoId { get; set; }
    public string NomeFantasia { get; set; } = string.Empty;
    public string DonoNome { get; set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; set; }
}
