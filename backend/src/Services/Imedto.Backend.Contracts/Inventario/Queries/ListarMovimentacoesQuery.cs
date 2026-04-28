using Imedto.Backend.Contracts.Inventario.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Queries;

public class ListarMovimentacoesQuery : IQuery<IEnumerable<MovimentacaoEstoqueDto>>
{
    public long EstabelecimentoId { get; set; }
    public long? ItemInventarioId { get; set; }
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public int Limite { get; set; } = 100;
}
