using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Commands;

public class RegistrarMovimentacaoEstoqueCommand : ICommand
{
    public long ItemInventarioId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Tipo { get; set; } = string.Empty; // "Entrada" | "Saida"
    public decimal Quantidade { get; set; }
    public string? Observacao { get; set; }
    public Guid UsuarioId { get; set; }
}
