namespace Imedto.Backend.Contracts.Relatorios.Queries.Results;

/// <summary>
/// Saída do <c>RelatorioPessoasQuery</c>. Substitui rpc_report_patients_summary e
/// rpc_report_professionals_performance. Apenas a sub-seção solicitada via <c>tipo</c>
/// vem preenchida; a outra fica <see langword="null"/>.
/// </summary>
public class RelatorioPessoasDto
{
    public string Tipo { get; set; } = string.Empty;
    public PacientesResumoDto? Pacientes { get; set; }
    public ProfissionaisResumoDto? Profissionais { get; set; }
}

public class PacientesResumoDto
{
    public int Novos { get; set; }
    public int Retornos { get; set; }
    public IList<RowSummary> PorFaixaEtaria { get; set; } = new List<RowSummary>();
    public IList<TopPacienteDto> TopAtivos { get; set; } = new List<TopPacienteDto>();
}

public class TopPacienteDto
{
    public long PacienteId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Atendimentos { get; set; }
}

public class ProfissionaisResumoDto
{
    public IList<DesempenhoProfissionalDto> Desempenho { get; set; } = new List<DesempenhoProfissionalDto>();
}

public class DesempenhoProfissionalDto
{
    public Guid ProfissionalUsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Atendimentos { get; set; }
    public int AtendimentosConcluidos { get; set; }
    public decimal Faturamento { get; set; }
    public decimal TaxaOcupacao { get; set; }
}
