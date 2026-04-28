using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Estabelecimentos.Events;

public record EstabelecimentoCriadoEvent(long EstabelecimentoId, Guid DonoUsuarioId, string NomeFantasia) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
