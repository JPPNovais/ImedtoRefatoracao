using Imedto.Backend.Contracts.Admin.Planos.Queries;
using Imedto.Backend.Contracts.Admin.Planos.Queries.Results;
using Imedto.Backend.Infrastructure.Admin;

namespace Imedto.Backend.Application.Admin.Planos;

public class ListarPlanosAdminQueryHandler
{
    private readonly ImedtoPlanoQueryRepository _queryRepo;

    public ListarPlanosAdminQueryHandler(ImedtoPlanoQueryRepository queryRepo)
    {
        _queryRepo = queryRepo;
    }

    public Task<ListarPlanosAdminResult> Handle(ListarPlanosAdminQuery query, CancellationToken ct = default)
        => _queryRepo.ListarAsync(query.Ativo, query.Busca, query.Pagina, query.Tamanho, ct);
}
