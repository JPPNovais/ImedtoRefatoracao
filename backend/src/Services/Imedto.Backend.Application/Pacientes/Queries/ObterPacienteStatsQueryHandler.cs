using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Pacientes.Queries;

public class ObterPacienteStatsQueryHandler : IRequestHandler<ObterPacienteStatsQuery, PacienteStatsDto>
{
    private readonly PacienteQueryRepository _queryRepository;

    public ObterPacienteStatsQueryHandler(PacienteQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<PacienteStatsDto> Handle(ObterPacienteStatsQuery query) =>
        _queryRepository.ObterStats(query.EstabelecimentoId);
}
