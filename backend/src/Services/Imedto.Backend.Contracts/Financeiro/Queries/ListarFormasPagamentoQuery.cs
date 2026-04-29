using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Queries;

public class ListarFormasPagamentoQuery : IQuery<IEnumerable<FormaPagamentoDto>>
{
    public long EstabelecimentoId { get; set; }
    public bool? Ativas { get; set; }
    public bool? Padrao { get; set; }
}
