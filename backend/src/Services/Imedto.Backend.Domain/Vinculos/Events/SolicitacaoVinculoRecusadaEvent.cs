using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Vinculos.Events;

/// <summary>
/// Solicitação recusada pelo dono. Consumido por handler de notificação ao profissional.
/// O motivo de recusa fica no aggregate (não vaza no event para minimizar PII em logs).
/// </summary>
public record SolicitacaoVinculoRecusadaEvent(
    long SolicitacaoId,
    Guid ProfissionalUsuarioId,
    long EstabelecimentoId) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
