using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Catalogo.Queries;

public class ListarRegioesCatalogoQueryHandlers : IRequestHandler<ListarRegioesCatalogoQuery, IEnumerable<RegiaoCatalogoDto>>
{
    private readonly CatalogoQueryRepository _repo;

    public ListarRegioesCatalogoQueryHandlers(CatalogoQueryRepository repo) => _repo = repo;

    public Task<IEnumerable<RegiaoCatalogoDto>> Handle(ListarRegioesCatalogoQuery query)
        => _repo.ListarRegioesCatalogo(query.Vista, query.ApenasAtivas);
}
