using Imedto.Backend.Contracts.Orcamentos.Queries;
using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Orcamentos.Queries;

public class ObterOrcamentoPorAgendamentoQueryHandler
    : IRequestHandler<ObterOrcamentoPorAgendamentoQuery, OrcamentoResumoDto?>
{
    private readonly OrcamentoQueryRepository _repo;

    public ObterOrcamentoPorAgendamentoQueryHandler(OrcamentoQueryRepository repo) => _repo = repo;

    public Task<OrcamentoResumoDto?> Handle(ObterOrcamentoPorAgendamentoQuery q)
        => _repo.ObterResumoPorAgendamento(q.AgendamentoId, q.EstabelecimentoId);
}
