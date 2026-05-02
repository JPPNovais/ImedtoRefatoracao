using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Prontuarios.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Events;

public class ProntuarioIniciadoEventHandler : IEventHandler<ProntuarioIniciadoEvent>
{
    private readonly ILogger<ProntuarioIniciadoEventHandler> _logger;
    public ProntuarioIniciadoEventHandler(ILogger<ProntuarioIniciadoEventHandler> logger) => _logger = logger;

    public Task Handle(ProntuarioIniciadoEvent domainEvent)
    {
        _logger.LogInformation("Prontuário iniciado: Id={ProntuarioId}, Paciente={PacienteId}, Estabelecimento={EstabelecimentoId}",
            domainEvent.ProntuarioId, domainEvent.PacienteId, domainEvent.EstabelecimentoId);
        return Task.CompletedTask;
    }
}

public class EvolucaoRegistradaEventHandler : IEventHandler<EvolucaoRegistradaEvent>
{
    private readonly ILogger<EvolucaoRegistradaEventHandler> _logger;
    public EvolucaoRegistradaEventHandler(ILogger<EvolucaoRegistradaEventHandler> logger) => _logger = logger;

    public Task Handle(EvolucaoRegistradaEvent domainEvent)
    {
        _logger.LogInformation("Evolução registrada: Id={EvolucaoId}, Prontuário={ProntuarioId}, Autor={AutorUsuarioId}",
            domainEvent.EvolucaoId, domainEvent.ProntuarioId, domainEvent.AutorUsuarioId);
        return Task.CompletedTask;
    }
}
