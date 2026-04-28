using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Inventario.Events;

public record EstoqueAbaixoMinimoEvent(
    long ItemInventarioId,
    long EstabelecimentoId,
    string ItemNome,
    decimal QuantidadeAtual,
    decimal QuantidadeMinima) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
