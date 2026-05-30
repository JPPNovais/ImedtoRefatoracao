using Imedto.Backend.Contracts.Admin.Catalogos.Regioes.Queries;
using Imedto.Backend.Contracts.Admin.Catalogos.Regioes.Queries.Results;
using Imedto.Backend.Infrastructure.Admin;

namespace Imedto.Backend.Application.Admin.Catalogos.Regioes;

public class ObterRegiaoGlobalQueryHandler
{
    private readonly ImedtoRegiaoAnatomicaGlobalQueryRepository _repo;

    public ObterRegiaoGlobalQueryHandler(ImedtoRegiaoAnatomicaGlobalQueryRepository repo) => _repo = repo;

    public Task<RegiaoGlobalDetalheDto?> Handle(ObterRegiaoGlobalQuery query, CancellationToken ct = default)
        => _repo.ObterPorIdAsync(query.Id, ct);
}
