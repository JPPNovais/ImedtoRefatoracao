using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Queries;

public class ListarModelosDescricaoCirurgicaQueryHandler
    : IRequestHandler<ListarModelosDescricaoCirurgicaQuery, IEnumerable<ModeloDescricaoCirurgicaDto>>
{
    private readonly ModeloDescricaoCirurgicaQueryRepository _queryRepository;

    public ListarModelosDescricaoCirurgicaQueryHandler(ModeloDescricaoCirurgicaQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<IEnumerable<ModeloDescricaoCirurgicaDto>> Handle(ListarModelosDescricaoCirurgicaQuery query) =>
        _queryRepository.Listar(query.EstabelecimentoId, query.ApenasAtivos);
}
