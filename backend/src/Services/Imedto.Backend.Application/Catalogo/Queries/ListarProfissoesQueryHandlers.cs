using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Catalogo.Queries;

public class ListarProfissoesQueryHandlers : IRequestHandler<ListarProfissoesQuery, IEnumerable<ProfissaoListadaDto>>
{
    private readonly CatalogoQueryRepository _repo;

    public ListarProfissoesQueryHandlers(CatalogoQueryRepository repo) => _repo = repo;

    public Task<IEnumerable<ProfissaoListadaDto>> Handle(ListarProfissoesQuery query)
        => _repo.ListarProfissoes(query.ApenasAtivas);
}
