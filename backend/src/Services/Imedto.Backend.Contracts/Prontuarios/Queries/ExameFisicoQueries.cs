using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

/// <summary>Retorna o exame físico completo (com regiões). 404 quando não existir.</summary>
public class ObterExameFisicoQuery : IQuery<ExameFisicoDto?>
{
    public long ExameFisicoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}

/// <summary>Retorna o exame físico associado a uma evolução (no fluxo do prontuário).</summary>
public class ObterExameFisicoPorEvolucaoQuery : IQuery<ExameFisicoDto?>
{
    public long EvolucaoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}

/// <summary>Lista paginada de exames físicos do paciente (sem regiões — versão leve).</summary>
public class ListarExamesFisicosDoPacienteQuery : IQuery<PaginaExamesFisicosDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public int Pagina { get; set; } = 1;
    public int Tamanho { get; set; } = 10;
}

/// <summary>Timeline curta dos N últimos exames físicos (versão leve).</summary>
public class TimelineExamesFisicosQuery : IQuery<IEnumerable<ExameFisicoResumoDto>>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public int Ate { get; set; } = 10;
}
