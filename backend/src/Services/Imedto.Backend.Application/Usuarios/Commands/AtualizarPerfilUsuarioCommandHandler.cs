using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Application.Usuarios.Commands;

public class AtualizarPerfilUsuarioCommandHandler : ICommandHandler<AtualizarPerfilUsuarioCommand>
{
    private readonly IUsuarioRepository _repository;
    private readonly ICurrentTenantAccessor _tenant;

    public AtualizarPerfilUsuarioCommandHandler(
        IUsuarioRepository repository,
        ICurrentTenantAccessor tenant)
    {
        _repository = repository;
        _tenant = tenant;
    }

    public async Task Handle(AtualizarPerfilUsuarioCommand command)
    {
        // Defense-in-depth completa: ignora command.UsuarioId e usa o sub do JWT
        // (populado pelo CurrentUserMiddleware). Garante que mesmo se um caller
        // futuro passar UsuarioId arbitrario, so o usuario autenticado eh editado.
        var usuarioIdJwt = _tenant.UsuarioId;
        if (usuarioIdJwt == Guid.Empty)
            throw new BusinessException("Usuário não autenticado.");

        var usuario = await _repository.ObterPorIdOuNulo(usuarioIdJwt)
            ?? throw new BusinessException("Usuário não encontrado.");
        usuario.AtualizarPerfil(command.NomeCompleto, command.Telefone);
        await _repository.Salvar(usuario);
    }
}
