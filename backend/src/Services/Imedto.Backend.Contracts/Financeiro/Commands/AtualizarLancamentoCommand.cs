using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Commands;

public class AtualizarLancamentoCommand : ICommand
{
    public long LancamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateOnly DataVencimento { get; set; }
    public string Categoria { get; set; } = string.Empty;
}
