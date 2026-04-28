using Imedto.Backend.Contracts.Unidades.Queries;
using Imedto.Backend.Contracts.Unidades.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Unidades.Queries;

public class ListarUnidadesQueryHandlers : IRequestHandler<ListarUnidadesQuery, IEnumerable<UnidadeDto>>
{
    private readonly UnidadeQueryRepository _queryRepository;

    public ListarUnidadesQueryHandlers(UnidadeQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<IEnumerable<UnidadeDto>> Handle(ListarUnidadesQuery query) =>
        _queryRepository.ListarPorEstabelecimento(query.EstabelecimentoId);
}
