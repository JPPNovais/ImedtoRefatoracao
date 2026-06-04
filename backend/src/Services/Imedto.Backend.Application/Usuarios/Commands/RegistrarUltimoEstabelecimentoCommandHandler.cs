using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Application.Usuarios.Commands;

/// <summary>
/// Persiste o último estabelecimento acessado pelo usuário autenticado.
/// Valida multi-tenant falha-fechada: o usuário deve ser Dono OU ter vínculo Ativo
/// com o estabelecimento informado. Sem acesso → BusinessException genérica.
/// </summary>
public class RegistrarUltimoEstabelecimentoCommandHandler : ICommandHandler<RegistrarUltimoEstabelecimentoCommand>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IVinculoRepository _vinculoRepository;
    private readonly ICurrentTenantAccessor _tenant;

    public RegistrarUltimoEstabelecimentoCommandHandler(
        IUsuarioRepository usuarioRepository,
        IVinculoRepository vinculoRepository,
        ICurrentTenantAccessor tenant)
    {
        _usuarioRepository = usuarioRepository;
        _vinculoRepository = vinculoRepository;
        _tenant = tenant;
    }

    public async Task Handle(RegistrarUltimoEstabelecimentoCommand command)
    {
        var usuarioId = _tenant.UsuarioId;
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário não autenticado.");

        // Falha-fechada: valida acesso antes de qualquer escrita.
        // PodeAtuarComoProfissional cobre: vínculo não-inativo OU é o dono.
        var temAcesso = await _vinculoRepository.PodeAtuarComoProfissional(usuarioId, command.EstabelecimentoId);
        if (!temAcesso)
            throw new BusinessException("Não encontrado.");

        var usuario = await _usuarioRepository.ObterPorIdOuNulo(usuarioId)
            ?? throw new BusinessException("Usuário não encontrado.");

        usuario.RegistrarUltimoEstabelecimento(command.EstabelecimentoId);
        await _usuarioRepository.Salvar(usuario);
    }
}
