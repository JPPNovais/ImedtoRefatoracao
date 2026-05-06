namespace Imedto.Backend.Contracts.Pacientes.Queries.Results;

/// <summary>
/// KPIs agregados de pacientes do estabelecimento atual. Exibidos no header da
/// lista. Os campos `emDebito` e `totalEmAberto` ficam zerados até a integração
/// financeira por paciente entregar — o front oculta o KPI quando o valor é
/// indisponível (manter como contrato versionável).
/// </summary>
public class PacienteStatsDto
{
    /// <summary>Total de pacientes ativos (não-deletados) no estabelecimento.</summary>
    public int Total { get; set; }

    /// <summary>Quantos foram cadastrados desde o primeiro dia do mês corrente (UTC).</summary>
    public int NovosMesCorrente { get; set; }
}
