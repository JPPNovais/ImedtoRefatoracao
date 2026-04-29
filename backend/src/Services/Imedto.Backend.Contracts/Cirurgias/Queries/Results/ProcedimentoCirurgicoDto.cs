using Imedto.Backend.Contracts.Cirurgias;

namespace Imedto.Backend.Contracts.Cirurgias.Queries.Results;

public class ProcedimentoCirurgicoResumoDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public long PacienteId { get; set; }
    public string PacienteNome { get; set; } = string.Empty;
    public long ProntuarioId { get; set; }
    public long? AgendamentoId { get; set; }
    public DateTime? DataAgendada { get; set; }
    public DateTime? DataRealizada { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CirurgiaPrincipal { get; set; } = string.Empty;
    public string? CirurgiaCodigo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class ProcedimentoCirurgicoDto : ProcedimentoCirurgicoResumoDto
{
    public string? DescricaoCirurgica { get; set; }
    public FichaAnestesica? FichaAnestesica { get; set; }
    public string? EvolucaoPosOp { get; set; }
    public string? Observacoes { get; set; }
    public DateTime? CanceladoEm { get; set; }
    public string? MotivoCancelamento { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public List<MembroEquipeCirurgicaDto> Equipe { get; set; } = new();
}

public class MembroEquipeCirurgicaDto
{
    public long Id { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    public string? ProfissionalNome { get; set; }
    public string Papel { get; set; } = string.Empty;
    public int Ordem { get; set; }
}
