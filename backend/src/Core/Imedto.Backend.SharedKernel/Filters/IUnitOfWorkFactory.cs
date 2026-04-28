namespace Imedto.Backend.SharedKernel.Filters;

public interface IUnitOfWorkFactory
{
    IUnitOfWorkScope Begin();
}

public interface IUnitOfWorkScope : IAsyncDisposable
{
    Task CommitAsync();
}
