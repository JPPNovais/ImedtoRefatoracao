using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.Regioes;

public class ListarArvoreRegioesAdminQueryHandler
{
    private readonly RegiaoAnatomicaAdminQueryRepository _repo;

    public ListarArvoreRegioesAdminQueryHandler(RegiaoAnatomicaAdminQueryRepository repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<RegiaoAnatomicaNoDto>> Handle(bool incluirInativas, CancellationToken ct = default)
        => _repo.ObterArvoreAsync(incluirInativas, ct);
}
