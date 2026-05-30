using Imedto.Backend.Contracts.Admin.Catalogos.Regioes.Queries;
using Imedto.Backend.Contracts.Admin.Catalogos.Regioes.Queries.Results;
using Imedto.Backend.Infrastructure.Admin;

namespace Imedto.Backend.Application.Admin.Catalogos.Regioes;

public class ListarRegioesGlobaisQueryHandler
{
    private readonly ImedtoRegiaoAnatomicaGlobalQueryRepository _repo;

    public ListarRegioesGlobaisQueryHandler(ImedtoRegiaoAnatomicaGlobalQueryRepository repo) => _repo = repo;

    public Task<(IReadOnlyList<RegiaoGlobalListaItemDto> Itens, int Total)> Handle(
        ListarRegioesGlobaisQuery query, CancellationToken ct = default)
        => _repo.ListarAsync(query.IncluirInativos, query.Busca, query.SistemaCorporal, query.Pagina, query.TamanhoPagina, ct);
}
