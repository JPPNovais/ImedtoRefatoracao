using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

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
        // Defesa minima: nunca aceitar Guid.Empty (vetor obvio de bypass).
        // Defense-in-depth completa (validar contra sub do JWT) requer mudanca
        // arquitetural — handler nao depende de HttpContext por design.
        if (command.UsuarioId == Guid.Empty)
            throw new BusinessException("Usuário não identificado.");

        var usuario = await _repository.ObterPorIdOuNulo(command.UsuarioId)
            ?? throw new BusinessException("Usuário não encontrado.");
        usuario.AtualizarPerfil(command.NomeCompleto, command.Telefone);
        await _repository.Salvar(usuario);
    }
}
