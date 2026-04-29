using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Financeiro.Queries;

public class ListarFormasPagamentoQueryHandlers
    : IRequestHandler<ListarFormasPagamentoQuery, IEnumerable<FormaPagamentoDto>>
{
    private readonly FormaPagamentoQueryRepository _repo;

    public ListarFormasPagamentoQueryHandlers(FormaPagamentoQueryRepository repo) => _repo = repo;

    public Task<IEnumerable<FormaPagamentoDto>> Handle(ListarFormasPagamentoQuery query)
        => _repo.Listar(query.EstabelecimentoId, query.Ativas, query.Padrao);
}
