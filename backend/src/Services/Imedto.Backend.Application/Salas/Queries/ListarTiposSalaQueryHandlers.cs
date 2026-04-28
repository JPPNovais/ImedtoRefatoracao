using Imedto.Backend.Contracts.Salas.Queries;
using Imedto.Backend.Contracts.Salas.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Salas.Queries;

public class ListarTiposSalaQueryHandlers : IRequestHandler<ListarTiposSalaQuery, IEnumerable<TipoSalaDto>>
{
    private readonly SalaQueryRepository _queryRepository;

    public ListarTiposSalaQueryHandlers(SalaQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<IEnumerable<TipoSalaDto>> Handle(ListarTiposSalaQuery query) =>
        _queryRepository.ListarTipos();
}
