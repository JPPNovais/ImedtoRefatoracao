namespace Imedto.Backend.Contracts.Agendamentos.Queries.Results;

public class PaginaAgendamentosDto
{
    public IEnumerable<AgendamentoDto> Itens { get; set; } = Array.Empty<AgendamentoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}

public class AgendamentoDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public long PacienteId { get; set; }
    public string PacienteNome { get; set; } = string.Empty;
    public Guid ProfissionalUsuarioId { get; set; }
    public string ProfissionalNome { get; set; } = string.Empty;
    /// <summary>URL presigned (S3) da foto do profissional, quando houver.</summary>
    public string? ProfissionalFotoUrl { get; set; }
    public string CriadoPorNome { get; set; } = string.Empty;
    public DateTime InicioPrevisto { get; set; }
    public DateTime FimPrevisto { get; set; }
    public string TipoServico { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? MotivoCancelamento { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public DateTime? CheckInEm { get; set; }
    public long? SalaId { get; set; }
    public string? SalaNome { get; set; }
    public string? SalaTipoNome { get; set; }

    // Badge de cobrança agregado na query (CA3 — sem N+1)
    public long? CobrancaId { get; set; }
    /// <summary>Aberta | ParcialmentePaga | Paga | Convenio | null (sem cobrança)</summary>
    public string? CobrancaStatus { get; set; }
    public decimal? CobrancaValorCobrado { get; set; }
    public decimal? CobrancaTotalPago { get; set; }
    public decimal? CobrancaSaldoDevedor { get; set; }

    /// <summary>
    /// Faixa etária derivada do paciente (D1 briefing 2026-06-23_002).
    /// Calculado na query Dapper a partir de pacientes.data_nascimento — sem expor a data completa.
    /// Valores: "idoso" (≥60 anos), "menor" (&lt;18 anos), null (adulto 18-59 ou sem data).
    /// </summary>
    public string? PacienteFaixaEtaria { get; set; }
}
