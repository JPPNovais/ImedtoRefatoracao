using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Vinculos.Events;

public class VinculoAceitoEventHandler : IEventHandler<VinculoAceitoEvent>
{
    private readonly ILogger<VinculoAceitoEventHandler> _logger;

    public VinculoAceitoEventHandler(ILogger<VinculoAceitoEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(VinculoAceitoEvent @event)
    {
        _logger.LogInformation(
            "Convite aceito: Vinculo={VinculoId}, Profissional={ProfissionalUsuarioId}, Estabelecimento={EstabelecimentoId}",
            @event.VinculoId, @event.ProfissionalUsuarioId, @event.EstabelecimentoId);
        return Task.CompletedTask;
    }
}
