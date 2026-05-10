using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Vinculos.Events;

/// <summary>
/// Disparado quando o modelo de permissão de um vínculo ativo é trocado.
/// Bridge de realtime usa esse evento para notificar o profissional afetado a
/// revalidar suas permissões — evita que o front continue exibindo menus que
/// não funcionam mais (backend já bloqueia 403, mas UI ficaria desincronizada).
/// </summary>
public record VinculoModeloPermissaoAlteradoEvent(
    long VinculoId,
    Guid ProfissionalUsuarioId,
    long EstabelecimentoId,
    long NovoModeloPermissaoId) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
