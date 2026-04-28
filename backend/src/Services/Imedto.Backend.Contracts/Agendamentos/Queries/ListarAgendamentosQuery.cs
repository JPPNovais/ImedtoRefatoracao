using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Agendamentos.Queries;

public class ListarAgendamentosQuery : IQuery<IEnumerable<AgendamentoDto>>
{
    public long EstabelecimentoId { get; set; }
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public Guid? ProfissionalUsuarioId { get; set; }
    public long? PacienteId { get; set; }
    public string? Status { get; set; }
}
