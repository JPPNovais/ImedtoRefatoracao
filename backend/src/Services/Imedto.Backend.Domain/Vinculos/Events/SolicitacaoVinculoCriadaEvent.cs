using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Vinculos.Events;

/// <summary>
/// Profissional solicitou acesso a um estabelecimento (fluxo inverso ao convite).
/// Disparado pelo aggregate após persistência. Consumidores: notificação ao dono.
/// </summary>
public record SolicitacaoVinculoCriadaEvent(
    long SolicitacaoId,
    Guid ProfissionalUsuarioId,
    long EstabelecimentoId) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
