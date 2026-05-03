using Microsoft.EntityFrameworkCore.Storage;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Filters;

namespace Imedto.Backend.Infrastructure.Database;

/// <summary>
/// Abre uma transação no AppDbContext por requisição. O scope é descartado
/// automaticamente pelo UnitOfWorkAttribute; se CommitAsync não for chamado
/// (action lançou), o Dispose faz rollback implícito.
/// </summary>
public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly AppDbContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public UnitOfWorkFactory(AppDbContext context, IDomainEventDispatcher eventDispatcher)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
    }

    public IUnitOfWorkScope Begin() => new EfUnitOfWorkScope(_context, _eventDispatcher);
}

internal sealed class EfUnitOfWorkScope : IUnitOfWorkScope
{
    private readonly AppDbContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IDbContextTransaction _transaction;
    private bool _committed;

    public EfUnitOfWorkScope(AppDbContext context, IDomainEventDispatcher eventDispatcher)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
        _transaction = context.Database.BeginTransaction();
    }

    public async Task CommitAsync()
    {
        await _context.SaveChangesAsync();

        // Despacha eventos de dominio APOS o save (handlers veem Ids gerados)
        // e ANTES do commit (falha de handler reverte a transacao).
        // Idempotente: handlers que ja chamaram ClearDomainEvents manualmente
        // nao geram duplicacao.
        // Se algum handler de evento mutar o ChangeTracker, segundo SaveChanges
        // garante persistencia antes do commit final.
        await _eventDispatcher.DispatchAsync();
        if (_context.ChangeTracker.HasChanges())
            await _context.SaveChangesAsync();

        await _transaction.CommitAsync();
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_committed)
            await _transaction.RollbackAsync();

        await _transaction.DisposeAsync();
    }
}
