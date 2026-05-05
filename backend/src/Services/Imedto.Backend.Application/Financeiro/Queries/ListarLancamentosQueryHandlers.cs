using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Financeiro.Queries;

public class ListarLancamentosQueryHandlers : IRequestHandler<ListarLancamentosQuery, PaginaLancamentosDto>
{
    private readonly FinanceiroQueryRepository _repo;

    public ListarLancamentosQueryHandlers(FinanceiroQueryRepository repo) => _repo = repo;

    public Task<PaginaLancamentosDto> Handle(ListarLancamentosQuery query)
        => _repo.Listar(
            query.EstabelecimentoId,
            query.Tipo,
            query.Status,
            query.Categoria,
            query.DataInicio,
            query.DataFim,
            query.Pagina,
            query.TamanhoPagina);
}
