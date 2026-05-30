using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.VariaveisPadraoSistema;

public class ObterVariavelPadraoSistemaQueryHandler
{
    private readonly VariavelPadraoSistemaQueryRepository _repo;

    public ObterVariavelPadraoSistemaQueryHandler(VariavelPadraoSistemaQueryRepository repo)
    {
        _repo = repo;
    }

    public Task<VariavelPadraoSistemaDetalheDto?> Handle(long id, CancellationToken ct = default)
        => _repo.ObterAsync(id, ct);
}
