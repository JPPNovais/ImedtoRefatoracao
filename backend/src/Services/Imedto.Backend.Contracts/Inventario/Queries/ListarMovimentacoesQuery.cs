using Imedto.Backend.Contracts.Inventario.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Queries;

public class ListarMovimentacoesQuery : IQuery<PaginaMovimentacoesEstoqueDto>
{
    public long EstabelecimentoId { get; set; }
    public long? ItemInventarioId { get; set; }
    public string? Tipo { get; set; }
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}
