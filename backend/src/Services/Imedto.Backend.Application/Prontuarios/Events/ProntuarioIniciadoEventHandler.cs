using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Prontuarios.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Events;

public class ProntuarioIniciadoEventHandler : IEventHandler<ProntuarioIniciadoEvent>
{
    private readonly ILogger<ProntuarioIniciadoEventHandler> _logger;
    public ProntuarioIniciadoEventHandler(ILogger<ProntuarioIniciadoEventHandler> logger) => _logger = logger;

    public Task Handle(ProntuarioIniciadoEvent @event)
    {
        _logger.LogInformation("Prontuário iniciado: Id={ProntuarioId}, Paciente={PacienteId}, Estabelecimento={EstabelecimentoId}",
            @event.ProntuarioId, @event.PacienteId, @event.EstabelecimentoId);
        return Task.CompletedTask;
    }
}

public class EvolucaoRegistradaEventHandler : IEventHandler<EvolucaoRegistradaEvent>
{
    private readonly ILogger<EvolucaoRegistradaEventHandler> _logger;
    public EvolucaoRegistradaEventHandler(ILogger<EvolucaoRegistradaEventHandler> logger) => _logger = logger;

    public Task Handle(EvolucaoRegistradaEvent @event)
    {
        _logger.LogInformation("Evolução registrada: Id={EvolucaoId}, Prontuário={ProntuarioId}, Autor={AutorUsuarioId}",
            @event.EvolucaoId, @event.ProntuarioId, @event.AutorUsuarioId);
        return Task.CompletedTask;
    }
}
