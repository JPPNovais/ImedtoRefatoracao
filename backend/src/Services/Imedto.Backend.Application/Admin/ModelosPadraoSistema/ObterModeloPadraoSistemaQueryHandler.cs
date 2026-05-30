using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.ModelosPadraoSistema;

public class ObterModeloPadraoSistemaQueryHandler
{
    private readonly ModeloPadraoSistemaQueryRepository _repo;

    public ObterModeloPadraoSistemaQueryHandler(ModeloPadraoSistemaQueryRepository repo)
    {
        _repo = repo;
    }

    public Task<ModeloPadraoSistemaDetalheDto?> Handle(long id, CancellationToken ct = default)
        => _repo.ObterAsync(id, ct);
}
