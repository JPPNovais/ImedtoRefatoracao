using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Financeiro.Queries;

public class ListarCategoriasFinanceirasQueryHandlers
    : IRequestHandler<ListarCategoriasFinanceirasQuery, IEnumerable<CategoriaFinanceiraDto>>
{
    private readonly CategoriaFinanceiraQueryRepository _repo;

    public ListarCategoriasFinanceirasQueryHandlers(CategoriaFinanceiraQueryRepository repo) => _repo = repo;

    public Task<IEnumerable<CategoriaFinanceiraDto>> Handle(ListarCategoriasFinanceirasQuery query)
        => _repo.Listar(query.EstabelecimentoId, query.Tipo, query.Ativas, query.Padrao);
}
