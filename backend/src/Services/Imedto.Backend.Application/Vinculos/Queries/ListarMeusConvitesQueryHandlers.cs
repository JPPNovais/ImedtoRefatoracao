using Imedto.Backend.Contracts.Vinculos.Queries;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Vinculos.Queries;

public class ListarMeusConvitesQueryHandlers : IRequestHandler<ListarMeusConvitesQuery, IEnumerable<ConviteDto>>
{
    private readonly VinculoQueryRepository _queryRepository;

    public ListarMeusConvitesQueryHandlers(VinculoQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<IEnumerable<ConviteDto>> Handle(ListarMeusConvitesQuery query) =>
        _queryRepository.ListarConvitesPendentes(query.UsuarioId);
}
