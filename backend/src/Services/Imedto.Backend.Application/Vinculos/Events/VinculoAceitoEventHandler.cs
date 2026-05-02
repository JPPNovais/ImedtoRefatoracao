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

    public Task Handle(VinculoAceitoEvent domainEvent)
    {
        _logger.LogInformation(
            "Convite aceito: Vinculo={VinculoId}, Profissional={ProfissionalUsuarioId}, Estabelecimento={EstabelecimentoId}",
            domainEvent.VinculoId, domainEvent.ProfissionalUsuarioId, domainEvent.EstabelecimentoId);
        return Task.CompletedTask;
    }
}
