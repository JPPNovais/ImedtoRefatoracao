using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Queries;

public class ListarVariaveisPoolQueryHandlers
    : IRequestHandler<ListarVariaveisPoolQuery, IEnumerable<VariavelPoolDto>>
{
    private readonly VariavelPoolQueryRepository _queryRepository;

    public ListarVariaveisPoolQueryHandlers(VariavelPoolQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<IEnumerable<VariavelPoolDto>> Handle(ListarVariaveisPoolQuery query) =>
        _queryRepository.Listar(query.EstabelecimentoId, query.Tipo, query.ApenasAtivos);
}
