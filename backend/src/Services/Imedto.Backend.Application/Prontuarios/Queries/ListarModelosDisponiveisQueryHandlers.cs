using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Queries;

public class ListarModelosDisponiveisQueryHandlers
    : IRequestHandler<ListarModelosDisponiveisQuery, IEnumerable<ModeloProntuarioDto>>
{
    private readonly ModeloProntuarioQueryRepository _queryRepository;

    public ListarModelosDisponiveisQueryHandlers(ModeloProntuarioQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<IEnumerable<ModeloProntuarioDto>> Handle(ListarModelosDisponiveisQuery query) =>
        _queryRepository.ListarDisponiveis(query.EstabelecimentoId, query.ApenasAtivos);
}
