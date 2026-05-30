using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.ModelosPadraoSistema;

public class ListarModelosPadraoSistemaQueryHandler
{
    private readonly ModeloPadraoSistemaQueryRepository _repo;

    public ListarModelosPadraoSistemaQueryHandler(ModeloPadraoSistemaQueryRepository repo)
    {
        _repo = repo;
    }

    public Task<(IEnumerable<ModeloPadraoSistemaListaItemDto> Itens, int Total)> Handle(
        bool incluirInativos,
        string? busca,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
        => _repo.ListarAsync(incluirInativos, busca, pagina, tamanhoPagina, ct);
}
