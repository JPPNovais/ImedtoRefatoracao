using Imedto.Backend.Contracts.Cirurgias.Queries;
using Imedto.Backend.Contracts.Cirurgias.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Cirurgias.Queries;

/// <summary>
/// Listagem agendada (calendário cirúrgico). Singleton — apenas leitura de metadados
/// não-clínicos (cirurgia principal, datas, status), sem necessidade de audit por item.
/// </summary>
public class ListarProcedimentosPlanejadosQueryHandlers
    : IRequestHandler<ListarProcedimentosPlanejadosQuery, IEnumerable<ProcedimentoCirurgicoResumoDto>>
{
    private readonly ProcedimentoCirurgicoQueryRepository _repo;

    public ListarProcedimentosPlanejadosQueryHandlers(ProcedimentoCirurgicoQueryRepository repo)
        => _repo = repo;

    public Task<IEnumerable<ProcedimentoCirurgicoResumoDto>> Handle(ListarProcedimentosPlanejadosQuery query)
        => _repo.ListarPlanejados(query.EstabelecimentoId, query.DataInicio, query.DataFim);
}
