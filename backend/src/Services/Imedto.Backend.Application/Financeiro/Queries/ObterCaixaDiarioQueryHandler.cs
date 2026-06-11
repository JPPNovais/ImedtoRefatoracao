using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Financeiro.Queries;

public class ObterCaixaDiarioQueryHandler : IRequestHandler<ObterCaixaDiarioQuery, CaixaDiarioDto?>
{
    private readonly ConsolidacaoFinanceiraQueryRepository _repo;

    public ObterCaixaDiarioQueryHandler(ConsolidacaoFinanceiraQueryRepository repo) => _repo = repo;

    public Task<CaixaDiarioDto?> Handle(ObterCaixaDiarioQuery query)
        => _repo.ObterCaixaDiario(query.EstabelecimentoId, query.Data);
}
