using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Catalogo.Queries;

public class ObterProcedimentoPorCodigoQueryHandlers : IRequestHandler<ObterProcedimentoPorCodigoQuery, ProcedimentoCatalogoDto?>
{
    private readonly ProcedimentoCatalogoQueryRepository _repo;

    public ObterProcedimentoPorCodigoQueryHandlers(ProcedimentoCatalogoQueryRepository repo) => _repo = repo;

    public Task<ProcedimentoCatalogoDto?> Handle(ObterProcedimentoPorCodigoQuery query)
        => _repo.ObterPorCodigo(query.Codigo);
}
