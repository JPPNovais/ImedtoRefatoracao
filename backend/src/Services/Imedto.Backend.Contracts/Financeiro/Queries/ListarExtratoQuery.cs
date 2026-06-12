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

    // Modo vencidos (R4/CA4): quando true, ignora DataInicio/DataFim e filtra
    // status='Pendente' AND data_vencimento < hoje. Comportamento idêntico ao
    // DashboardQueryRepository (paridade CA13). Sem este flag, comportamento inalterado (CA15).
    public bool SomenteVencidos { get; set; }

    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}
