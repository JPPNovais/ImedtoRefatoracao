using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Microsoft.Extensions.Caching.Memory;

namespace Imedto.Backend.Application.Catalogo.Queries;

public class ListarRegioesCatalogoQueryHandlers : IRequestHandler<ListarRegioesCatalogoQuery, IEnumerable<RegiaoCatalogoDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private readonly CatalogoQueryRepository _repo;
    private readonly IMemoryCache _cache;

    public ListarRegioesCatalogoQueryHandlers(CatalogoQueryRepository repo, IMemoryCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    // Payload pesado (>100 linhas com JSON de coordenadas SVG) — cache evita serializar
    // a mesma resposta a cada abertura do BodyMap do exame físico.
    public Task<IEnumerable<RegiaoCatalogoDto>> Handle(ListarRegioesCatalogoQuery query)
    {
        var key = $"catalogo:regioes:vista={query.Vista ?? "all"}:ativas={query.ApenasAtivas}";
        return _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await _repo.ListarRegioesCatalogo(query.Vista, query.ApenasAtivas);
        })!;
    }
}
