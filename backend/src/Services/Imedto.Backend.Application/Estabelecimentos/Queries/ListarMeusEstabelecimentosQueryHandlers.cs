using Imedto.Backend.Contracts.Estabelecimentos.Queries;
using Imedto.Backend.Contracts.Estabelecimentos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Estabelecimentos.Queries;

public class ListarMeusEstabelecimentosQueryHandlers : IRequestHandler<ListarMeusEstabelecimentosQuery, IEnumerable<EstabelecimentoDto>>
{
    private readonly EstabelecimentoQueryRepository _queryRepository;

    public ListarMeusEstabelecimentosQueryHandlers(EstabelecimentoQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<IEnumerable<EstabelecimentoDto>> Handle(ListarMeusEstabelecimentosQuery query) =>
        await _queryRepository.ListarPorUsuario(query.UsuarioId);
}
