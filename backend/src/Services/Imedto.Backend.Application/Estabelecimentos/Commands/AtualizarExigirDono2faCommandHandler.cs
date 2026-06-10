using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;

// ForbiddenException → HTTP 403 via GlobalExceptionFilter (ver SharedKernel/Domain/ForbiddenException.cs)

namespace Imedto.Backend.Application.Estabelecimentos.Commands;

/// <summary>
/// Atualiza o toggle "Exigir 2FA para o papel Dono" (R9/CA13/CA15).
/// Restrito ao Dono — verificado via ICurrentTenantAccessor.EhDono.
/// Multi-tenant: afeta apenas o estabelecimento do contexto da request.
/// </summary>
public class AtualizarExigirDono2faCommandHandler : ICommandHandler<AtualizarExigirDono2faCommand>
{
    private readonly IEstabelecimentoRepository _repo;
    private readonly ICurrentTenantAccessor _tenant;

    public AtualizarExigirDono2faCommandHandler(
        IEstabelecimentoRepository repo,
        ICurrentTenantAccessor tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    public async Task Handle(AtualizarExigirDono2faCommand cmd)
    {
        // RBAC: apenas Dono pode alterar (R9/CA13)
        if (!_tenant.EhDono)
            throw new ForbiddenException("Sem permissão para alterar configurações de segurança.");

        // Multi-tenant: o ID do comando deve ser o do tenant ativo (falha-fechada)
        if (cmd.EstabelecimentoId != _tenant.EstabelecimentoId)
            throw new ForbiddenException("Sem permissão para alterar configurações de segurança.");

        var estabelecimento = await _repo.ObterPorId(cmd.EstabelecimentoId)
            ?? throw new BusinessException("Estabelecimento não encontrado.");

        estabelecimento.AtualizarExigirDono2fa(cmd.Exigir);
        await _repo.Salvar(estabelecimento);
    }
}
