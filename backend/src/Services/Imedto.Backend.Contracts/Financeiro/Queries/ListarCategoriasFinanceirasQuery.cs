using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Queries;

public class ListarCategoriasFinanceirasQuery : IQuery<IEnumerable<CategoriaFinanceiraDto>>
{
    public long EstabelecimentoId { get; set; }
    public string? Tipo { get; set; }      // "Receita" | "Despesa" | null = ambos
    public bool? Ativas { get; set; }      // null = todos, true = só ativas, false = só inativas
    public bool? Padrao { get; set; }      // null = todos, true = só padrão, false = só customizadas
}
