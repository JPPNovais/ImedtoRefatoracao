using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Vinculos.Events;

public class ProfissionalConvidadoEventHandler : IEventHandler<ProfissionalConvidadoEvent>
{
    private readonly ILogger<ProfissionalConvidadoEventHandler> _logger;

    public ProfissionalConvidadoEventHandler(ILogger<ProfissionalConvidadoEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ProfissionalConvidadoEvent @event)
    {
        _logger.LogInformation(
            "Convite criado: Vinculo={VinculoId}, Profissional={ProfissionalUsuarioId}, Estabelecimento={EstabelecimentoId}",
            @event.VinculoId, @event.ProfissionalUsuarioId, @event.EstabelecimentoId);
        return Task.CompletedTask;
    }
}
