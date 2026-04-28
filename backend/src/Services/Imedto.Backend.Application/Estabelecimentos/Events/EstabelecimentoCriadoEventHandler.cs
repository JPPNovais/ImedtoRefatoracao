using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Estabelecimentos.Events;

public class EstabelecimentoCriadoEventHandler : IEventHandler<EstabelecimentoCriadoEvent>
{
    private readonly ILogger<EstabelecimentoCriadoEventHandler> _logger;

    public EstabelecimentoCriadoEventHandler(ILogger<EstabelecimentoCriadoEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(EstabelecimentoCriadoEvent @event)
    {
        _logger.LogInformation(
            "Estabelecimento criado: Id={EstabelecimentoId}, Dono={DonoUsuarioId}, Nome={NomeFantasia}",
            @event.EstabelecimentoId, @event.DonoUsuarioId, @event.NomeFantasia);
        return Task.CompletedTask;
    }
}
