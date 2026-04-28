using Imedto.Backend.Contracts.ModelosPermissao.Queries;
using Imedto.Backend.Contracts.ModelosPermissao.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.ModelosPermissao.Queries;

public class ListarModelosPermissaoQueryHandlers : IRequestHandler<ListarModelosPermissaoQuery, IEnumerable<ModeloPermissaoDto>>
{
    private readonly ModeloPermissaoQueryRepository _repo;

    public ListarModelosPermissaoQueryHandlers(ModeloPermissaoQueryRepository repo)
        => _repo = repo;

    public Task<IEnumerable<ModeloPermissaoDto>> Handle(ListarModelosPermissaoQuery query)
        => _repo.ListarPorEstabelecimento(query.EstabelecimentoId);
}
