using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Microsoft.Extensions.Caching.Memory;

namespace Imedto.Backend.Application.Catalogo.Queries;

public class ListarEspecialidadesQueryHandlers : IRequestHandler<ListarEspecialidadesQuery, IEnumerable<EspecialidadeListadaDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private readonly CatalogoQueryRepository _repo;
    private readonly IMemoryCache _cache;

    public ListarEspecialidadesQueryHandlers(CatalogoQueryRepository repo, IMemoryCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public Task<IEnumerable<EspecialidadeListadaDto>> Handle(ListarEspecialidadesQuery query)
    {
        var key = $"catalogo:especialidades:profissao={query.ProfissaoId}:ativas={query.ApenasAtivas}";
        return _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await _repo.ListarEspecialidades(query.ProfissaoId, query.ApenasAtivas);
        })!;
    }
}
