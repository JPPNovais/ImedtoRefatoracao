using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cirurgias.Commands;

public class CancelarProcedimentoCommand : ICommand
{
    public long ProcedimentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
