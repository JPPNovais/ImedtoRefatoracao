using Imedto.Backend.Contracts.Inventario.Queries;
using Imedto.Backend.Contracts.Inventario.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Inventario.Queries;

public class ListarMovimentacoesQueryHandlers : IRequestHandler<ListarMovimentacoesQuery, PaginaMovimentacoesEstoqueDto>
{
    private readonly InventarioQueryRepository _repo;

    public ListarMovimentacoesQueryHandlers(InventarioQueryRepository repo)
        => _repo = repo;

    public Task<PaginaMovimentacoesEstoqueDto> Handle(ListarMovimentacoesQuery query)
        => _repo.ListarMovimentacoes(
            query.EstabelecimentoId,
            query.ItemInventarioId,
            query.DataInicio,
            query.DataFim,
            query.Pagina,
            query.TamanhoPagina);
}
