using Imedto.Backend.Contracts.Admin.Catalogos.Variaveis.Queries;
using Imedto.Backend.Contracts.Admin.Catalogos.Variaveis.Queries.Results;
using Imedto.Backend.Infrastructure.Admin;

namespace Imedto.Backend.Application.Admin.Catalogos.Variaveis;

public class ListarVariaveisGlobaisQueryHandler
{
    private readonly ImedtoVariavelPoolGlobalQueryRepository _repo;

    public ListarVariaveisGlobaisQueryHandler(ImedtoVariavelPoolGlobalQueryRepository repo) => _repo = repo;

    public Task<(IReadOnlyList<VariavelGlobalListaItemDto> Itens, int Total)> Handle(
        ListarVariaveisGlobaisQuery query, CancellationToken ct = default)
        => _repo.ListarAsync(query.IncluirInativos, query.Busca, query.Tipo, query.Pagina, query.TamanhoPagina, ct);
}
