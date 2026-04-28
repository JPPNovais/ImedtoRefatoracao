using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Commands;

public class CriarLancamentoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Tipo { get; set; } = string.Empty; // "Receita" | "Despesa"
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateOnly DataVencimento { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public long? OrcamentoId { get; set; }
    public Guid CriadoPorUsuarioId { get; set; }
}
