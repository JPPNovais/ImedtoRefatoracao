using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Notificacoes.Events;

/// <summary>
/// Disparado pelo <c>INotificacaoService</c> após persistir uma notificação.
/// Consumidores típicos: realtime (SignalR) e — futuramente — push mobile.
///
/// IMPORTANTE: a engine de automações (item 2.2) NÃO escuta este evento — caso contrário
/// teríamos loop quando uma regra envia notificação que dispara outra regra que envia notificação.
/// </summary>
public record NotificacaoCriadaEvent(
    long NotificacaoId,
    Guid UsuarioId,
    long? EstabelecimentoId,
    string Titulo,
    string Mensagem,
    CategoriaNotificacao Categoria,
    string? LinkAcao) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
