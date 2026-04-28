using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Infrastructure.Bus;

/// <summary>
/// Bus de commands em memória. O registro de handlers é compartilhado (singleton),
/// mas os handlers são resolvidos do scope da request atual para compartilhar
/// o DbContext com o UnitOfWorkAttribute.
/// </summary>
public class MemoryCommandBus : ICommandBus
{
    private readonly IServiceProvider _rootProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Dictionary<Type, Type> _handlers = new();

    public MemoryCommandBus(IServiceProvider rootProvider, IHttpContextAccessor httpContextAccessor)
    {
        _rootProvider = rootProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Register<TCommand>(ICommandHandler<TCommand> handler) where TCommand : ICommand
    {
        _handlers[typeof(TCommand)] = handler.GetType();
    }

    public void Register<TCommand, THandler>()
        where TCommand : ICommand
        where THandler : ICommandHandler<TCommand>
    {
        _handlers[typeof(TCommand)] = typeof(THandler);
    }

    public async Task Send<TCommand>(TCommand command) where TCommand : ICommand
    {
        if (!_handlers.TryGetValue(typeof(TCommand), out var handlerType))
            throw new InvalidOperationException($"Nenhum handler registrado para {typeof(TCommand).Name}.");

        var serviceProvider = _httpContextAccessor.HttpContext?.RequestServices ?? _rootProvider;
        var handler = (ICommandHandler<TCommand>)serviceProvider.GetRequiredService(handlerType);
        await handler.Handle(command);
    }
}
