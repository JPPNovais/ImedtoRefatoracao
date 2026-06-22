using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

/// <summary>
/// Atualiza os alertas clínicos do paciente dentro do contexto do prontuário.
/// Gated por R3 (LGPD briefing 2026-06-22_002): apenas Dono (sempre) ou
/// Profissional que atendeu/está atendendo o paciente.
/// Auditado como TipoAcessoProntuario.Escrita via IProntuarioAcessoLogService.
/// </summary>
public class AtualizarAlertasProntuarioCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public TenantPapel SolicitantePapel { get; set; }

    /// <summary>
    /// Lista completa de alertas após a edição (substitui o array inteiro).
    /// Máximo 10 itens; cada item máx. 200 caracteres.
    /// </summary>
    public IReadOnlyList<string> Alertas { get; set; } = Array.Empty<string>();
}
