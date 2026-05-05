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
    public string CriadoPorNome { get; set; } = string.Empty;
    public DateTime InicioPrevisto { get; set; }
    public DateTime FimPrevisto { get; set; }
    public string TipoServico { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? MotivoCancelamento { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}
