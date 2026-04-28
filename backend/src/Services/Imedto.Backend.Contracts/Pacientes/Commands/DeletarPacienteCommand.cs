using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Commands;

/// <summary>
/// Soft delete (LGPD — direito ao esquecimento). Mantém registro marcado com
/// <c>deletado_em</c> para retenção legal mínima; listagens excluem automaticamente.
/// </summary>
public class DeletarPacienteCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}
