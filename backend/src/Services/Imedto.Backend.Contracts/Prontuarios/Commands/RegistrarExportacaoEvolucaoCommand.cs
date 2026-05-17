using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

/// <summary>
/// Audit LGPD — registra que uma evolução específica foi exportada (PDF).
/// O front chama este comando ANTES de gerar o doc; se falhar, o PDF não é
/// produzido. Defense-in-depth multi-tenant: valida tenant + vínculo
/// evolução↔prontuário↔paciente no handler.
/// </summary>
public class RegistrarExportacaoEvolucaoCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EvolucaoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}
