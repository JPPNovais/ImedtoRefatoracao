using Imedto.Backend.Contracts.Admin.Catalogos.Modelos.Queries;
using Imedto.Backend.Contracts.Admin.Catalogos.Modelos.Queries.Results;
using Imedto.Backend.Infrastructure.Admin;

namespace Imedto.Backend.Application.Admin.Catalogos.Modelos;

public class ObterModeloGlobalQueryHandler
{
    private readonly ImedtoModeloProntuarioGlobalQueryRepository _repo;

    public ObterModeloGlobalQueryHandler(ImedtoModeloProntuarioGlobalQueryRepository repo) => _repo = repo;

    public Task<ModeloGlobalDetalheDto?> Handle(ObterModeloGlobalQuery query, CancellationToken ct = default)
        => _repo.ObterPorIdAsync(query.Id, ct);
}
