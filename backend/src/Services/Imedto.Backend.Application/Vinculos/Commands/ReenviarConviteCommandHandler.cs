using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

/// <summary>
/// Reenvia o e-mail de convite para um vínculo em status <see cref="VinculoStatus.Convidado"/>.
/// Multi-tenant: filtra o vínculo pelo <c>EstabelecimentoId</c> da request (defesa em profundidade —
/// o atributo <c>RequiresPermissaoExtra</c> já valida a permissão no estabelecimento, mas
/// confirmamos aqui que o <c>VinculoId</c> realmente pertence ao tenant antes de tocar em qualquer dado).
/// Cooldown anti-spam (5 min) é aplicado dentro do <see cref="IAuthService"/>.
/// </summary>
public class ReenviarConviteCommandHandler : ICommandHandler<ReenviarConviteCommand>
{
    private readonly IVinculoRepository _vinculoRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IAuthService _authService;

    public ReenviarConviteCommandHandler(
        IVinculoRepository vinculoRepo,
        IUsuarioRepository usuarioRepo,
        IAuthService authService)
    {
        _vinculoRepo = vinculoRepo;
        _usuarioRepo = usuarioRepo;
        _authService = authService;
    }

    public async Task Handle(ReenviarConviteCommand command)
    {
        var vinculo = await _vinculoRepo.ObterPorIdNoEstabelecimentoOuNulo(command.VinculoId, command.EstabelecimentoId)
            ?? throw new BusinessException("Convite não encontrado.");

        if (vinculo.Status != VinculoStatus.Convidado)
            throw new BusinessException("Este vínculo não está mais pendente — não há convite para reenviar.");

        var usuario = await _usuarioRepo.ObterPorIdOuNulo(vinculo.ProfissionalUsuarioId)
            ?? throw new BusinessException("Convidado não encontrado.");

        if (string.IsNullOrWhiteSpace(usuario.Email))
            throw new BusinessException("Convidado não possui e-mail cadastrado.");

        await _authService.ReenviarConviteAsync(usuario.Email);
    }
}
