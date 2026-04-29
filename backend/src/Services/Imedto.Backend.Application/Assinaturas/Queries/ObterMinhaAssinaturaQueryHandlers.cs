using Imedto.Backend.Contracts.Assinaturas.Queries;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Assinaturas.Queries;

public class ObterMinhaAssinaturaQueryHandlers : IRequestHandler<ObterMinhaAssinaturaQuery, AssinaturaDto?>
{
    private readonly AssinaturaQueryRepository _repo;

    public ObterMinhaAssinaturaQueryHandlers(AssinaturaQueryRepository repo) => _repo = repo;

    public Task<AssinaturaDto?> Handle(ObterMinhaAssinaturaQuery query)
        => _repo.ObterDoEstabelecimento(query.EstabelecimentoId);
}
