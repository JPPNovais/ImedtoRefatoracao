using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Queries;

public class ListarExtratoQuery : IQuery<PaginaLancamentosExtratoDto>
{
    public long EstabelecimentoId { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }

    // Filtros (R4 — aplicados no backend via WHERE)
    public string? Tipo { get; set; }
    public string? Categoria { get; set; }
    public string? FormaPagamento { get; set; }
    public string? Origem { get; set; }

    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}
