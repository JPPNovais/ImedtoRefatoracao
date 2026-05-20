using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Termos.Events;

/// <summary>Disparado quando o estabelecimento revoga um termo já assinado.</summary>
public record TermoRevogadoEvent(
    long TermoEmitidoId,
    long PacienteId,
    long EstabelecimentoId,
    Guid RevogadoPorUsuarioId) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
