using Imedto.Backend.Contracts.Admin.Assinaturas.Queries;
using Imedto.Backend.Contracts.Admin.Assinaturas.Queries.Results;
using Imedto.Backend.Infrastructure.Admin;

namespace Imedto.Backend.Application.Admin.Assinaturas;

public class ListarHistoricoAssinaturasAdminQueryHandler
{
    private readonly ImedtoAssinaturaQueryRepository _queryRepo;

    public ListarHistoricoAssinaturasAdminQueryHandler(ImedtoAssinaturaQueryRepository queryRepo)
    {
        _queryRepo = queryRepo;
    }

    public Task<IReadOnlyList<AssinaturaAdminDto>> Handle(
        ListarHistoricoAssinaturasAdminQuery query,
        CancellationToken ct = default)
        => _queryRepo.ListarHistoricoAsync(query.EstabelecimentoId, ct);
}
