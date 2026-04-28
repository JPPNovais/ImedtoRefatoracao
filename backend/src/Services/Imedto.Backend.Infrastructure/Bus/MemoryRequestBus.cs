using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Infrastructure.Bus;

/// <summary>
/// Bus de queries em memória. Handlers resolvidos do scope da request atual.
/// </summary>
public class MemoryRequestBus : IRequestBus
{
    private readonly IServiceProvider _rootProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Dictionary<Type, (Type HandlerType, Type ResultType)> _handlers = new();

    public MemoryRequestBus(IServiceProvider rootProvider, IHttpContextAccessor httpContextAccessor)
    {
        _rootProvider = rootProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Register<TQuery, TResult>(IRequestHandler<TQuery, TResult> handler) where TQuery : IQuery<TResult>
    {
        _handlers[typeof(TQuery)] = (handler.GetType(), typeof(TResult));
    }

    public void Register<TQuery, TResult, THandler>()
        where TQuery : IQuery<TResult>
        where THandler : IRequestHandler<TQuery, TResult>
    {
        _handlers[typeof(TQuery)] = (typeof(THandler), typeof(TResult));
    }

    public async Task<TResult> Query<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
    {
        if (!_handlers.TryGetValue(typeof(TQuery), out var entry))
            throw new InvalidOperationException($"Nenhum handler registrado para {typeof(TQuery).Name}.");

        var serviceProvider = _httpContextAccessor.HttpContext?.RequestServices ?? _rootProvider;
        var handler = (IRequestHandler<TQuery, TResult>)serviceProvider.GetRequiredService(entry.HandlerType);
        return await handler.Handle(query);
    }
}
