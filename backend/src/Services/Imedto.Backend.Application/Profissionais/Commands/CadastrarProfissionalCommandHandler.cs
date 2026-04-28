using Imedto.Backend.Contracts.Profissionais.Commands;
using Imedto.Backend.Domain.Profissionais;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Profissionais.Commands;

public class CadastrarProfissionalCommandHandler : ICommandHandler<CadastrarProfissionalCommand>
{
    private readonly IProfissionalRepository _repository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IEventBus _eventBus;

    public CadastrarProfissionalCommandHandler(
        IProfissionalRepository repository,
        IUsuarioRepository usuarioRepository,
        IEventBus eventBus)
    {
        _repository = repository;
        _usuarioRepository = usuarioRepository;
        _eventBus = eventBus;
    }

    public async Task Handle(CadastrarProfissionalCommand command)
    {
        var usuario = await _usuarioRepository.ObterPorId(command.UsuarioId);

        if (string.IsNullOrWhiteSpace(usuario.Cpf))
            throw new BusinessException("Complete o onboarding (nome e CPF) antes de se cadastrar como profissional.");

        var existente = await _repository.ObterPorIdOuNulo(command.UsuarioId);
        if (existente is not null)
            throw new BusinessException("Você já possui cadastro profissional. Use atualizar para modificá-lo.");

        var conselho = (command.Conselho ?? "").Trim().ToUpperInvariant();
        var uf = (command.Uf ?? "").Trim().ToUpperInvariant();
        var numero = (command.NumeroRegistro ?? "").Trim();

        if (!string.IsNullOrWhiteSpace(conselho) && !string.IsNullOrWhiteSpace(uf) && !string.IsNullOrWhiteSpace(numero)
            && await _repository.ExisteConselhoRegistro(conselho, uf, numero, command.UsuarioId))
        {
            throw new BusinessException("Já existe outro profissional com este número de registro neste conselho/UF.");
        }

        var prof = Profissional.Cadastrar(
            command.UsuarioId,
            command.Conselho,
            command.Uf,
            command.NumeroRegistro,
            command.Especialidade,
            command.Bio);

        await _repository.Salvar(prof);

        foreach (var domainEvent in prof.DomainEvents)
            await _eventBus.Publish(domainEvent);

        prof.ClearDomainEvents();
    }
}
