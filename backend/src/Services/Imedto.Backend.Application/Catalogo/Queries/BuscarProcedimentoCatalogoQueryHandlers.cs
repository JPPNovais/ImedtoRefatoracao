using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Catalogo.Queries;

public class BuscarProcedimentoCatalogoQueryHandlers : IRequestHandler<BuscarProcedimentoCatalogoQuery, IEnumerable<ProcedimentoCatalogoDto>>
{
    private readonly ProcedimentoCatalogoQueryRepository _repo;

    public BuscarProcedimentoCatalogoQueryHandlers(ProcedimentoCatalogoQueryRepository repo) => _repo = repo;

    public Task<IEnumerable<ProcedimentoCatalogoDto>> Handle(BuscarProcedimentoCatalogoQuery query)
        => _repo.Buscar(query.Termo, query.Origem, query.Ativo, Math.Clamp(query.Limit, 1, 100));
}
