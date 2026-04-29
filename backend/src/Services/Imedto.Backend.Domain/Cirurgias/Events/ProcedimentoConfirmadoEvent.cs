using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Cirurgias.Events;

/// <summary>
/// Disparado quando um procedimento cirúrgico transita para <c>Confirmado</c>.
/// Consumido por handlers que precisam notificar a equipe (ver
/// <c>NotificarEquipeAoConfirmarHandler</c>).
/// O payload já carrega a equipe — evita o handler ter que reconsultar o aggregate.
/// </summary>
public record ProcedimentoConfirmadoEvent(
    long ProcedimentoId,
    long EstabelecimentoId,
    long PacienteId,
    string CirurgiaPrincipal,
    DateTime? DataAgendada,
    IReadOnlyList<Guid> MembrosEquipeUsuarioIds) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
