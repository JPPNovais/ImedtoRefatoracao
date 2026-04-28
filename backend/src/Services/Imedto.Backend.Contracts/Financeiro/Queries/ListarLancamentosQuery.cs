using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Queries;

public class ListarLancamentosQuery : IQuery<IEnumerable<LancamentoDto>>
{
    public long EstabelecimentoId { get; set; }
    public string? Tipo { get; set; }
    public string? Status { get; set; }
    public string? Categoria { get; set; }
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
}
