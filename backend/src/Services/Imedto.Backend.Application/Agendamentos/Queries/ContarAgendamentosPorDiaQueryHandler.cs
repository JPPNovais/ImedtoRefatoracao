using Imedto.Backend.Contracts.Agendamentos.Queries;
using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Agendamentos.Queries;

public class ContarAgendamentosPorDiaQueryHandler : IRequestHandler<ContarAgendamentosPorDiaQuery, IEnumerable<ContagemPorDiaDto>>
{
    private readonly AgendamentoQueryRepository _repo;

    public ContarAgendamentosPorDiaQueryHandler(AgendamentoQueryRepository repo)
        => _repo = repo;

    public Task<IEnumerable<ContagemPorDiaDto>> Handle(ContarAgendamentosPorDiaQuery query)
        => _repo.ContarPorDia(
            query.EstabelecimentoId,
            query.DataInicio,
            query.DataFim,
            query.ProfissionalUsuarioId);
}
