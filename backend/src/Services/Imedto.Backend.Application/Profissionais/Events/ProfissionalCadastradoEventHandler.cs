using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Profissionais.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Profissionais.Events;

public class ProfissionalCadastradoEventHandler : IEventHandler<ProfissionalCadastradoEvent>
{
    private readonly ILogger<ProfissionalCadastradoEventHandler> _logger;

    public ProfissionalCadastradoEventHandler(ILogger<ProfissionalCadastradoEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ProfissionalCadastradoEvent domainEvent)
    {
        _logger.LogInformation(
            "Profissional cadastrado: Usuario={UsuarioId}, {Conselho}/{Uf} {NumeroRegistro}",
            domainEvent.UsuarioId, domainEvent.Conselho, domainEvent.Uf, domainEvent.NumeroRegistro);
        return Task.CompletedTask;
    }
}
