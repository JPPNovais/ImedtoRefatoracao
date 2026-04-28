using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Queries;

public class ObterModeloDeProntuarioQueryHandlers : IRequestHandler<ObterModeloDeProntuarioQuery, ModeloProntuarioDto>
{
    private readonly ModeloProntuarioQueryRepository _queryRepository;

    public ObterModeloDeProntuarioQueryHandlers(ModeloProntuarioQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<ModeloProntuarioDto> Handle(ObterModeloDeProntuarioQuery query) =>
        _queryRepository.ObterVisivelPara(query.ModeloId, query.EstabelecimentoId);
}
