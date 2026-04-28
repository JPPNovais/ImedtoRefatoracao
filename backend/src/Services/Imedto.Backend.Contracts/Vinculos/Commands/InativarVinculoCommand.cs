using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Commands;

public class InativarVinculoCommand : ICommand
{
    public long VinculoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}
