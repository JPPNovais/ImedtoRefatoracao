using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Pacientes.Queries;

public class ObterPacienteQueryHandlers : IRequestHandler<ObterPacienteQuery, PacienteDto>
{
    private readonly PacienteQueryRepository _queryRepository;

    public ObterPacienteQueryHandlers(PacienteQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<PacienteDto> Handle(ObterPacienteQuery query) =>
        _queryRepository.ObterPorId(query.PacienteId, query.EstabelecimentoId);
}
