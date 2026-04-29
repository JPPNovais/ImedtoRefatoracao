using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Commands;

public class CancelarSolicitacaoVinculoCommand : ICommand
{
    public long SolicitacaoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}
