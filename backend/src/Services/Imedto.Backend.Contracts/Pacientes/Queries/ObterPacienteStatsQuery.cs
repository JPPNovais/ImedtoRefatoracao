using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Queries;

public class ObterPacienteStatsQuery : IQuery<PacienteStatsDto>
{
    public long EstabelecimentoId { get; set; }
}
