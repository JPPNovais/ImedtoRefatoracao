using Imedto.Backend.Contracts.Relatorios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Relatorios;

public class RelatorioAgendamentosQueryHandlers : IRequestHandler<RelatorioAgendamentosQuery, RelatorioAgendamentosDto>
{
    private readonly RelatorioQueryRepository _repo;

    public RelatorioAgendamentosQueryHandlers(RelatorioQueryRepository repo) => _repo = repo;

    public Task<RelatorioAgendamentosDto> Handle(RelatorioAgendamentosQuery query)
        => _repo.RelatorioAgendamentos(query.EstabelecimentoId, query.DataInicio, query.DataFim);
}
