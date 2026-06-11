namespace Imedto.Backend.Contracts.Financeiro.Queries.Results;

/// <summary>
/// Comissão por profissional no período (R17/R18).
/// Calculada 100% no backend — nunca somada no front.
/// </summary>
public class ComissaoPeriodoDto
{
    public decimal TotalARepassar { get; set; }
    public IEnumerable<ComissaoProfissionalDto> Profissionais { get; set; }
        = Array.Empty<ComissaoProfissionalDto>();
}

public class ComissaoProfissionalDto
{
    public Guid ProfissionalUsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Especialidade { get; set; }
    public int Atendimentos { get; set; }
    public decimal Faturamento { get; set; }
    public decimal? PercentualConfig { get; set; } // null = cirurgia (valor fixo OrcamentoEquipe)
    public decimal Comissao { get; set; }
    public IEnumerable<ComissaoAtendimentoDto> Atendimentos_Detalhes { get; set; }
        = Array.Empty<ComissaoAtendimentoDto>();
}

public class ComissaoAtendimentoDto
{
    public DateOnly Data { get; set; }
    public string TipoAtendimento { get; set; } = string.Empty; // Consulta|Procedimento|Cirurgia
    public long? PacienteId { get; set; }
    public string? PacienteNome { get; set; }
    public decimal Base { get; set; }
    public decimal Faturamento { get; set; }
    public decimal Comissao { get; set; }
    /// <summary>
    /// "percentual" (config ou padrão) ou "orcamento_equipe" (cirurgia).
    /// </summary>
    public string TipoBase { get; set; } = string.Empty;
}
