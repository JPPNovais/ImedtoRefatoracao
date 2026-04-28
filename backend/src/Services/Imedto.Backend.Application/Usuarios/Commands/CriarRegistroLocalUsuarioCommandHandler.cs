using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Usuarios.Commands;

public class CriarRegistroLocalUsuarioCommandHandler : ICommandHandler<CriarRegistroLocalUsuarioCommand>
{
    private readonly IUsuarioRepository _repository;
    private readonly IEventBus _eventBus;

    public CriarRegistroLocalUsuarioCommandHandler(IUsuarioRepository repository, IEventBus eventBus)
    {
        _repository = repository;
        _eventBus = eventBus;
    }

    public async Task Handle(CriarRegistroLocalUsuarioCommand command)
    {
        // Idempotência: se o registro já existe (ex.: retry de signup), só registra acesso.
        var existente = await _repository.ObterPorIdOuNulo(command.Id);
        if (existente is not null)
        {
            existente.RegistrarAcesso();
            await _repository.Salvar(existente);
            return;
        }

        var usuario = Usuario.Criar(command.Id, command.Email);
        await _repository.Salvar(usuario);

        foreach (var domainEvent in usuario.DomainEvents)
            await _eventBus.Publish(domainEvent);

        usuario.ClearDomainEvents();
    }
}
