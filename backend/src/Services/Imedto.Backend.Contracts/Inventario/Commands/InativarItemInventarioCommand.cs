using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Commands;

public class InativarItemInventarioCommand : ICommand
{
    public long ItemId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioId { get; set; }
    public string? Observacao { get; set; }
}
