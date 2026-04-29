using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Commands;

public class CriarCategoriaFinanceiraCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty; // "Receita" | "Despesa"
}
