using Imedto.Backend.Contracts.Vinculos.Queries;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Vinculos.Queries;

public class ListarMinhasSolicitacoesVinculoQueryHandlers
    : IRequestHandler<ListarMinhasSolicitacoesVinculoQuery, IEnumerable<SolicitacaoVinculoDto>>
{
    private readonly SolicitacaoVinculoQueryRepository _queryRepository;

    public ListarMinhasSolicitacoesVinculoQueryHandlers(SolicitacaoVinculoQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<IEnumerable<SolicitacaoVinculoDto>> Handle(ListarMinhasSolicitacoesVinculoQuery query)
        => _queryRepository.ListarPorProfissional(query.ProfissionalUsuarioId);
}
