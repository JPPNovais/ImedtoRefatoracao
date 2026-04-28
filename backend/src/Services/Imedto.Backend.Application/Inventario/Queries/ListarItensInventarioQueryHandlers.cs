using Imedto.Backend.Contracts.Inventario.Queries;
using Imedto.Backend.Contracts.Inventario.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Inventario.Queries;

public class ListarItensInventarioQueryHandlers : IRequestHandler<ListarItensInventarioQuery, IEnumerable<ItemInventarioDto>>
{
    private readonly InventarioQueryRepository _repo;

    public ListarItensInventarioQueryHandlers(InventarioQueryRepository repo)
        => _repo = repo;

    public Task<IEnumerable<ItemInventarioDto>> Handle(ListarItensInventarioQuery query)
        => _repo.ListarItens(query.EstabelecimentoId, query.Categoria, query.ApenasAbaixoMinimo, query.ApenasAtivos);
}
