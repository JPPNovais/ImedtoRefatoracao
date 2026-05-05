using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Microsoft.Extensions.Caching.Memory;

namespace Imedto.Backend.Application.Catalogo.Queries;

public class ListarProfissoesQueryHandlers : IRequestHandler<ListarProfissoesQuery, IEnumerable<ProfissaoListadaDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private readonly CatalogoQueryRepository _repo;
    private readonly IMemoryCache _cache;

    public ListarProfissoesQueryHandlers(CatalogoQueryRepository repo, IMemoryCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    // Catálogo nacional, muda raramente. Cache reduz round-trip ao Postgres no carregamento
    // de qualquer formulário que liste profissões (cadastro de profissional, filtro de busca).
    public Task<IEnumerable<ProfissaoListadaDto>> Handle(ListarProfissoesQuery query)
    {
        var key = $"catalogo:profissoes:ativas={query.ApenasAtivas}";
        return _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await _repo.ListarProfissoes(query.ApenasAtivas);
        })!;
    }
}
