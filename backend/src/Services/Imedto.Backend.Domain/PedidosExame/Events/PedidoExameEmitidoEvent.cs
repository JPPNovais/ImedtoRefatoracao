using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.PedidosExame.Events;

public record PedidoExameEmitidoEvent(
    long PedidoExameId,
    long PacienteId,
    long EstabelecimentoId,
    Guid ProfissionalUsuarioId,
    TipoPedidoExame Tipo,
    int QuantidadeExames) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
