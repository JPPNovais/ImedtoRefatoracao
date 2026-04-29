using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Vinculos.Events;

/// <summary>
/// Solicitação aprovada pelo dono. Consumido por:
/// 1. Handler que cria o <see cref="VinculoProfissionalEstabelecimento"/> automaticamente (status Ativo).
/// 2. Handler que envia notificação ao profissional.
/// </summary>
public record SolicitacaoVinculoAprovadaEvent(
    long SolicitacaoId,
    Guid ProfissionalUsuarioId,
    long EstabelecimentoId,
    Guid AprovadoPorUsuarioId) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
