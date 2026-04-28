using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Usuarios.Commands;

public class AtualizarPerfilUsuarioCommandHandler : ICommandHandler<AtualizarPerfilUsuarioCommand>
{
    private readonly IUsuarioRepository _repository;

    public AtualizarPerfilUsuarioCommandHandler(IUsuarioRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(AtualizarPerfilUsuarioCommand command)
    {
        var usuario = await _repository.ObterPorId(command.UsuarioId);
        usuario.AtualizarPerfil(command.NomeCompleto, command.Telefone);
        await _repository.Salvar(usuario);
    }
}
