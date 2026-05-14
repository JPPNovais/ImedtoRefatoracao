using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Pacientes.Queries;

public class BuscaRapidaPacientesQueryHandler : IRequestHandler<BuscaRapidaPacientesQuery, IReadOnlyList<PacienteBuscaRapidaDto>>
{
    private readonly PacienteQueryRepository _queryRepository;

    public BuscaRapidaPacientesQueryHandler(PacienteQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<IReadOnlyList<PacienteBuscaRapidaDto>> Handle(BuscaRapidaPacientesQuery query) =>
        _queryRepository.BuscaRapida(query.EstabelecimentoId, query.Q, query.Limite);
}
