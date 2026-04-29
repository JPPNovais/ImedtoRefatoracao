using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Commands;

public class RegistrarMovimentacaoEstoqueCommand : ICommand
{
    public long ItemInventarioId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Tipo { get; set; } = string.Empty; // "Entrada" | "Saida"
    public decimal Quantidade { get; set; }
    /// <summary>
    /// Custo unitário (R$/unidade). Obrigatório (>0) para Entrada — recalcula custo médio.
    /// Ignorado em Saída — handler usa o CustoMedio atual do item como snapshot.
    /// </summary>
    public decimal CustoUnitario { get; set; }
    public string? Observacao { get; set; }
    public Guid UsuarioId { get; set; }
}
