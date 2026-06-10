using Imedto.Backend.Contracts.Cobrancas.Queries;
using Imedto.Backend.Contracts.Cobrancas.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories.Cobrancas;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Cobrancas.Queries;

public class ListarConfigTaxaFormaPagamentoQueryHandlers : IRequestHandler<ListarConfigTaxaFormaPagamentoQuery, IEnumerable<ConfigTaxaFormaPagamentoDto>>
{
    private readonly CobrancaQueryRepository _repo;

    public ListarConfigTaxaFormaPagamentoQueryHandlers(CobrancaQueryRepository repo)
        => _repo = repo;

    public Task<IEnumerable<ConfigTaxaFormaPagamentoDto>> Handle(ListarConfigTaxaFormaPagamentoQuery query)
        => _repo.ListarConfigTaxa(query.EstabelecimentoId);
}
