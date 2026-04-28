using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Financeiro.Queries;

public class ObterResumoFinanceiroQueryHandlers : IRequestHandler<ObterResumoFinanceiroQuery, ResumoFinanceiroDto>
{
    private readonly FinanceiroQueryRepository _repo;

    public ObterResumoFinanceiroQueryHandlers(FinanceiroQueryRepository repo) => _repo = repo;

    public Task<ResumoFinanceiroDto> Handle(ObterResumoFinanceiroQuery query)
        => _repo.ObterResumo(query.EstabelecimentoId, query.DataInicio, query.DataFim);
}
