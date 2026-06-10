using Imedto.Backend.Contracts.Cobrancas.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cobrancas.Queries;

public class ListarConfigTaxaFormaPagamentoQuery : IQuery<IEnumerable<ConfigTaxaFormaPagamentoDto>>
{
    public long EstabelecimentoId { get; set; }
}
