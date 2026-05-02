using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Infrastructure.Bus;

/// <summary>
/// Bus de events em memória (síncrono). Handlers resolvidos do scope da request atual.
/// Para eventos distribuídos, substituir por MassTransit/Kafka.
/// </summary>
public class MemoryEventBus : IEventBus
{
    private readonly IServiceProvider _rootProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Dictionary<Type, List<Type>> _handlers = new();

    public MemoryEventBus(IServiceProvider rootProvider, IHttpContextAccessor httpContextAccessor)
    {
        _rootProvider = rootProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Register<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        if (!_handlers.ContainsKey(typeof(TEvent)))
            _handlers[typeof(TEvent)] = new List<Type>();

        _handlers[typeof(TEvent)].Add(handler.GetType());
    }

    public void Register<TEvent, THandler>()
        where TEvent : IDomainEvent
        where THandler : IEventHandler<TEvent>
    {
        if (!_handlers.ContainsKey(typeof(TEvent)))
            _handlers[typeof(TEvent)] = new List<Type>();

        _handlers[typeof(TEvent)].Add(typeof(THandler));
    }

    public async Task Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        // Usa o runtime type do evento — quando o caller itera uma coleção de IDomainEvent,
        // typeof(TEvent) seria IDomainEvent e o dispatch perderia todos os handlers concretos.
        var eventType = domainEvent!.GetType();

        if (!_handlers.TryGetValue(eventType, out var handlerTypes))
            return;

        var serviceProvider = _httpContextAccessor.HttpContext?.RequestServices ?? _rootProvider;
        var handleMethod = typeof(IEventHandler<>)
            .MakeGenericType(eventType)
            .GetMethod(nameof(IEventHandler<IDomainEvent>.Handle));

        foreach (var handlerType in handlerTypes)
        {
            var handler = serviceProvider.GetRequiredService(handlerType);
            await (Task)handleMethod!.Invoke(handler, new object[] { domainEvent })!;
        }
    }
}
