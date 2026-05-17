using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

/// <summary>
/// Audit LGPD — registra que o histórico completo do prontuário foi exportado
/// (PDF). O front chama este comando ANTES de gerar o doc; se falhar, o PDF não
/// é produzido. Defense-in-depth multi-tenant e validação de acesso no handler.
/// </summary>
public class RegistrarExportacaoProntuarioCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}
