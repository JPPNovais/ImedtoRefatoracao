using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Queries;

public class ListarExtratoQueryHandler : IRequestHandler<ListarExtratoQuery, PaginaLancamentosExtratoDto>
{
    private readonly ConsolidacaoFinanceiraQueryRepository _repo;

    public ListarExtratoQueryHandler(ConsolidacaoFinanceiraQueryRepository repo) => _repo = repo;

    public Task<PaginaLancamentosExtratoDto> Handle(ListarExtratoQuery query)
    {
        if (query.Pagina < 1) throw new BusinessException("Página deve ser maior ou igual a 1.");
        if (query.TamanhoPagina < 1 || query.TamanhoPagina > 100)
            throw new BusinessException("Tamanho da página deve estar entre 1 e 100.");

        return _repo.ListarExtrato(
            query.EstabelecimentoId,
            query.DataInicio,
            query.DataFim,
            query.Tipo,
            query.Categoria,
            query.FormaPagamento,
            query.Origem,
            query.Pagina,
            query.TamanhoPagina);
    }
}
