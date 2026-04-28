namespace Imedto.Backend.SharedKernel.Cqrs;

/// <summary>
/// Handler de query CQRS.
/// Nomeie a classe como *QueryHandlers (plural) e registre no Container:
/// queryBus.Register&lt;MinhaQuery, MeuResultado, MeuQueryHandlers&gt;()
/// </summary>
public interface IRequestHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query);
}
