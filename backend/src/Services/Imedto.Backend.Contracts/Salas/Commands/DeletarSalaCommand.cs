using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Salas.Commands;

public class DeletarSalaCommand : ICommand
{
    public long SalaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}
