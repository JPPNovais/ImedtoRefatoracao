using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Financeiro.Queries;

public class ObterKpisFinanceiroQueryHandler : IRequestHandler<ObterKpisFinanceiroQuery, KpisFinanceiroDto>
{
    private readonly ConsolidacaoFinanceiraQueryRepository _repo;

    public ObterKpisFinanceiroQueryHandler(ConsolidacaoFinanceiraQueryRepository repo) => _repo = repo;

    public Task<KpisFinanceiroDto> Handle(ObterKpisFinanceiroQuery query)
        => _repo.ObterKpis(query.EstabelecimentoId, query.DataInicio, query.DataFim);
}
