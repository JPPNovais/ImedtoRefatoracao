using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

/// <summary>
/// Remove (soft-delete) um anexo do prontuário. O blob S3 é retido conforme política LGPD;
/// o registro recebe <c>deletado_em</c> e some das listagens.
/// Somente o autor do anexo ou o Dono pode remover (gating autor-ou-dono do briefing 001).
/// </summary>
public class RemoverAnexoCommand : ICommand
{
    public long AnexoId { get; set; }
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public TenantPapel SolicitantePapel { get; set; }
}
