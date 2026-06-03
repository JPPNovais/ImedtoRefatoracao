using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Agendamentos.Events;

/// <summary>
/// Disparado quando um agendamento tem horário ou profissional alterado via <c>Agendamento.Atualizar()</c>.
/// Gatilho: mudança em <c>InicioPrevisto</c>, <c>FimPrevisto</c> ou <c>ProfissionalUsuarioId</c>.
/// Aplicável independente do status de origem (Agendado ou Confirmado).
///
/// LGPD: sem PII — apenas IDs e novo horário.
/// O handler carrega nome fantasia/profissional/paciente apenas para montar o e-mail.
/// </summary>
public record AgendamentoReagendadoEvent(
    long AgendamentoId,
    long EstabelecimentoId,
    long PacienteId,
    Guid ProfissionalUsuarioId,
    DateTime NovoInicioPrevisto) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
