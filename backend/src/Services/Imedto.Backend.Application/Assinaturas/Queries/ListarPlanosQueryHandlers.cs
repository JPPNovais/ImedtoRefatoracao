using Imedto.Backend.Contracts.Assinaturas.Queries;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Assinaturas.Queries;

public class ListarPlanosQueryHandlers : IRequestHandler<ListarPlanosQuery, IEnumerable<PlanoDto>>
{
    private readonly PlanoQueryRepository _repo;

    public ListarPlanosQueryHandlers(PlanoQueryRepository repo) => _repo = repo;

    public Task<IEnumerable<PlanoDto>> Handle(ListarPlanosQuery query) => _repo.ListarAtivos();
}
