using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Queries;

public class ListarLancamentosQuery : IQuery<PaginaLancamentosDto>
{
    public long EstabelecimentoId { get; set; }
    public string? Tipo { get; set; }
    public string? Status { get; set; }
    public string? Categoria { get; set; }
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}
