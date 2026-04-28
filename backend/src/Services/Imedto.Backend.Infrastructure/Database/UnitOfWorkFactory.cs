using Microsoft.EntityFrameworkCore.Storage;
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

    public UnitOfWorkFactory(AppDbContext context)
    {
        _context = context;
    }

    public IUnitOfWorkScope Begin() => new EfUnitOfWorkScope(_context);
}

internal sealed class EfUnitOfWorkScope : IUnitOfWorkScope
{
    private readonly AppDbContext _context;
    private readonly IDbContextTransaction _transaction;
    private bool _committed;

    public EfUnitOfWorkScope(AppDbContext context)
    {
        _context = context;
        _transaction = context.Database.BeginTransaction();
    }

    public async Task CommitAsync()
    {
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
