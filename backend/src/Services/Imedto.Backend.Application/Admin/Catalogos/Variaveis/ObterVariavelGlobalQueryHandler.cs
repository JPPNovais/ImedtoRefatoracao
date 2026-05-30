using Imedto.Backend.Contracts.Admin.Catalogos.Variaveis.Queries;
using Imedto.Backend.Contracts.Admin.Catalogos.Variaveis.Queries.Results;
using Imedto.Backend.Infrastructure.Admin;

namespace Imedto.Backend.Application.Admin.Catalogos.Variaveis;

public class ObterVariavelGlobalQueryHandler
{
    private readonly ImedtoVariavelPoolGlobalQueryRepository _repo;

    public ObterVariavelGlobalQueryHandler(ImedtoVariavelPoolGlobalQueryRepository repo) => _repo = repo;

    public Task<VariavelGlobalDetalheDto?> Handle(ObterVariavelGlobalQuery query, CancellationToken ct = default)
        => _repo.ObterPorIdAsync(query.Id, ct);
}
