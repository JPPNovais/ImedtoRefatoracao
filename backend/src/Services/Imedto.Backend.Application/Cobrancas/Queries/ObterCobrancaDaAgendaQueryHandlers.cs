using Imedto.Backend.Contracts.Cobrancas.Queries;
using Imedto.Backend.Contracts.Cobrancas.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories.Cobrancas;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Cobrancas.Queries;

public class ObterCobrancaDaAgendaQueryHandlers : IRequestHandler<ObterCobrancaDaAgendaQuery, CobrancaDetalheDto?>
{
    private readonly CobrancaQueryRepository _repo;

    public ObterCobrancaDaAgendaQueryHandlers(CobrancaQueryRepository repo)
        => _repo = repo;

    public Task<CobrancaDetalheDto?> Handle(ObterCobrancaDaAgendaQuery query)
        => _repo.ObterDetalhesPorAgendamento(query.AgendamentoId, query.EstabelecimentoId);
}
