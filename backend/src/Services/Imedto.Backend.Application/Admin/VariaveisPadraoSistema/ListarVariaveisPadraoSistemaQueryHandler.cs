using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.VariaveisPadraoSistema;

public class ListarVariaveisPadraoSistemaQueryHandler
{
    private readonly VariavelPadraoSistemaQueryRepository _repo;

    public ListarVariaveisPadraoSistemaQueryHandler(VariavelPadraoSistemaQueryRepository repo)
    {
        _repo = repo;
    }

    public Task<(IEnumerable<VariavelPadraoSistemaListaItemDto> Itens, int Total)> Handle(
        bool incluirInativos,
        string? busca,
        string? categoria,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
        => _repo.ListarAsync(incluirInativos, busca, categoria, pagina, tamanhoPagina, ct);
}
