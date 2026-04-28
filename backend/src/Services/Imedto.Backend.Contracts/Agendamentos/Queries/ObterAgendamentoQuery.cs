using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Agendamentos.Queries;

public class ObterAgendamentoQuery : IQuery<AgendamentoDto>
{
    public long AgendamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
}
