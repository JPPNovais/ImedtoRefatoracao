using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.Regioes;

public class ObterRegiaoAdminQueryHandler
{
    private readonly RegiaoAnatomicaAdminQueryRepository _repo;

    public ObterRegiaoAdminQueryHandler(RegiaoAnatomicaAdminQueryRepository repo)
    {
        _repo = repo;
    }

    public Task<RegiaoAnatomicaNoDto?> Handle(long id, CancellationToken ct = default)
        => _repo.ObterPorIdAsync(id, ct);
}
