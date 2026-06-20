using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Catalogo.Queries;

public class BuscarCid10QueryHandlers : IRequestHandler<BuscarCid10Query, IEnumerable<Cid10Dto>>
{
    private readonly Cid10QueryRepository _repo;

    public BuscarCid10QueryHandlers(Cid10QueryRepository repo) => _repo = repo;

    public Task<IEnumerable<Cid10Dto>> Handle(BuscarCid10Query query)
        => _repo.Buscar(query.Busca, Math.Clamp(query.Limite, 1, 50));
}
