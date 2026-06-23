using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.CategoriasFinanceiras;

/// <summary>
/// Lista o catálogo global de categorias financeiras padrão para o admin.
/// Singleton — sem escopo de tenant, sem DbContext.
/// Briefing 2026-06-22_003 — M3.
/// </summary>
public class ListarCategoriasFinanceirasPadraoSistemaQueryHandler
{
    private readonly CategoriaFinanceiraPadraoSistemaQueryRepository _query;

    public ListarCategoriasFinanceirasPadraoSistemaQueryHandler(
        CategoriaFinanceiraPadraoSistemaQueryRepository query)
        => _query = query;

    public async Task<(IEnumerable<CategoriaFinanceiraPadraoListaItemDto> Itens, int Total)> Handle(
        string? tipo,
        bool? ativas,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
        => await _query.ListarAsync(tipo, ativas, pagina, tamanhoPagina, ct);
}
