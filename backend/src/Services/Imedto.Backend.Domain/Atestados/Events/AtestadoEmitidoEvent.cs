using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Atestados.Events;

/// <summary>
/// Disparado após emitir e persistir um atestado. Pode ser ouvido por engine
/// de automações (ex.: "ao emitir afastamento, gerar tarefa de acompanhamento")
/// ou para registrar audit/notification.
/// </summary>
public record AtestadoEmitidoEvent(
    long AtestadoId,
    long PacienteId,
    long EstabelecimentoId,
    Guid ProfissionalUsuarioId,
    TipoAtestado Tipo) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
