namespace Imedto.Backend.SharedKernel.Cqrs;

/// <summary>
/// Bus para execução de queries. Injetado nos controllers.
/// </summary>
public interface IRequestBus
{
    void Register<TQuery, TResult>(IRequestHandler<TQuery, TResult> handler) where TQuery : IQuery<TResult>;
    Task<TResult> Query<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>;
}
