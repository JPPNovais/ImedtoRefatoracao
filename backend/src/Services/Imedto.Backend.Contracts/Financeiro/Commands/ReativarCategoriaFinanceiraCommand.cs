using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Commands;

public class ReativarCategoriaFinanceiraCommand : ICommand
{
    public long CategoriaId { get; set; }
    public long EstabelecimentoId { get; set; }
}
