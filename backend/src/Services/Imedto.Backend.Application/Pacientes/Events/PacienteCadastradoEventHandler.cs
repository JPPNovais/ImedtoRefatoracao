using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Pacientes.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Pacientes.Events;

public class PacienteCadastradoEventHandler : IEventHandler<PacienteCadastradoEvent>
{
    private readonly ILogger<PacienteCadastradoEventHandler> _logger;

    public PacienteCadastradoEventHandler(ILogger<PacienteCadastradoEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PacienteCadastradoEvent @event)
    {
        // Não logar o nome (PII) — apenas IDs.
        _logger.LogInformation(
            "Paciente cadastrado: Id={PacienteId}, Estabelecimento={EstabelecimentoId}",
            @event.PacienteId, @event.EstabelecimentoId);
        return Task.CompletedTask;
    }
}
