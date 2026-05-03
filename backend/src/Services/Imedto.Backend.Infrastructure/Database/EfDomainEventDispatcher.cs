using Microsoft.EntityFrameworkCore;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Infrastructure.Database;

/// <summary>
/// Implementacao do <see cref="IDomainEventDispatcher"/> baseada no
/// ChangeTracker do <see cref="AppDbContext"/>. Coleta eventos de aggregates
/// (<c>Entity&lt;TId&gt;</c>) e publica via <see cref="IEventBus"/>.
///
/// Coleta vs Publish: faz copia da lista ANTES de publicar para o caso de
/// um handler de evento adicionar novos aggregates ao DbContext (recursao
/// natural — proximo Dispatch pega).
///
/// Eventos sao publicados sequencialmente (await em ordem). Se algum
/// handler lancar, o commit do UoW eh abortado (transacao revertida).
/// </summary>
public sealed class EfDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly AppDbContext _context;
    private readonly IEventBus _eventBus;

    public EfDomainEventDispatcher(AppDbContext context, IEventBus eventBus)
    {
        _context = context;
        _eventBus = eventBus;
    }

    public async Task DispatchAsync()
    {
        // Aggregates com Entity<long> ou Entity<Guid> — coletar TODOS rastreados.
        // Filtra por entry.Entity para evitar instanceof generico custoso.
        var aggregates = _context.ChangeTracker
            .Entries()
            .Select(e => e.Entity)
            .OfType<IDomainEventCarrier>()
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        if (aggregates.Count == 0) return;

        // Snapshot dos eventos antes de limpar — protege contra handlers que
        // mutam o ChangeTracker (adicionam novos aggregates) durante o loop.
        var eventos = aggregates.SelectMany(a => a.DomainEvents).ToList();

        // Limpa antes de publicar para impedir re-publish em caso de DispatchAsync
        // ser chamado de novo na mesma request (idempotencia).
        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        foreach (var ev in eventos)
            await _eventBus.Publish(ev);
    }
}

