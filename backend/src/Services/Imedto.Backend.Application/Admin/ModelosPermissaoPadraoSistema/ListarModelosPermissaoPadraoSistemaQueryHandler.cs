using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.ModelosPermissaoPadraoSistema;

/// <summary>
/// Query singleton — lista modelos de permissão padrão do sistema com paginação e busca.
/// CA15, CA16 do briefing 2026-06-04_001.
/// </summary>
public class ListarModelosPermissaoPadraoSistemaQueryHandler
{
    private readonly ModeloPermissaoPadraoSistemaQueryRepository _query;

    public ListarModelosPermissaoPadraoSistemaQueryHandler(ModeloPermissaoPadraoSistemaQueryRepository query)
    {
        _query = query;
    }

    public async Task<(IEnumerable<ModeloPermissaoPadraoListaItemDto> Itens, int Total)> Handle(
        string? busca,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
        => await _query.ListarAsync(busca, pagina, tamanhoPagina, ct);
}
