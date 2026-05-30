using Imedto.Backend.Contracts.Admin.Catalogos.Modelos.Queries;
using Imedto.Backend.Contracts.Admin.Catalogos.Modelos.Queries.Results;
using Imedto.Backend.Infrastructure.Admin;

namespace Imedto.Backend.Application.Admin.Catalogos.Modelos;

public class ListarModelosGlobaisQueryHandler
{
    private readonly ImedtoModeloProntuarioGlobalQueryRepository _repo;

    public ListarModelosGlobaisQueryHandler(ImedtoModeloProntuarioGlobalQueryRepository repo) => _repo = repo;

    public Task<(IReadOnlyList<ModeloGlobalListaItemDto> Itens, int Total)> Handle(
        ListarModelosGlobaisQuery query, CancellationToken ct = default)
        => _repo.ListarAsync(query.IncluirInativos, query.Busca, query.Pagina, query.TamanhoPagina, ct);
}
