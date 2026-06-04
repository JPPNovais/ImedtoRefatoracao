using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.ModelosPermissaoPadraoSistema;

/// <summary>
/// Query singleton — obtém detalhe de um modelo de permissão padrão do sistema.
/// </summary>
public class ObterModeloPermissaoPadraoSistemaQueryHandler
{
    private readonly ModeloPermissaoPadraoSistemaQueryRepository _query;

    public ObterModeloPermissaoPadraoSistemaQueryHandler(ModeloPermissaoPadraoSistemaQueryRepository query)
    {
        _query = query;
    }

    public async Task<ModeloPermissaoPadraoDetalheDto?> Handle(long id, CancellationToken ct = default)
        => await _query.ObterAsync(id, ct);
}
