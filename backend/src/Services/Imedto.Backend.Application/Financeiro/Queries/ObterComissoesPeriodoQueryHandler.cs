using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Financeiro.Queries;

public class ObterComissoesPeriodoQueryHandler : IRequestHandler<ObterComissoesPeriodoQuery, ComissaoPeriodoDto>
{
    private readonly ConsolidacaoFinanceiraQueryRepository _repo;

    public ObterComissoesPeriodoQueryHandler(ConsolidacaoFinanceiraQueryRepository repo) => _repo = repo;

    public Task<ComissaoPeriodoDto> Handle(ObterComissoesPeriodoQuery query)
        => _repo.ObterComissoes(query.EstabelecimentoId, query.DataInicio, query.DataFim);
}
