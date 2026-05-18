using Imedto.Backend.Contracts.Salas.Queries;
using Imedto.Backend.Contracts.Salas.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Salas.Queries;

public class ListarSalasQueryHandlers : IRequestHandler<ListarSalasQuery, IEnumerable<SalaDto>>
{
    private readonly SalaQueryRepository _queryRepository;

    public ListarSalasQueryHandlers(SalaQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<IEnumerable<SalaDto>> Handle(ListarSalasQuery query) =>
        _queryRepository.ListarPorEstabelecimento(query.EstabelecimentoId, query.ApenasAtivas);
}
