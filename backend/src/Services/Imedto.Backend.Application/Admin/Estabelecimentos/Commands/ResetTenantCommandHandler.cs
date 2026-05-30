using Imedto.Backend.Contracts.Admin.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
// AcoesAuditAdmin está em Imedto.Backend.Domain.Admin (importado acima)

namespace Imedto.Backend.Application.Admin.Estabelecimentos.Commands;

/// <summary>
/// Reseta dados do tenant com confirmação dupla (CA32–CA36):
/// 1. Valida que <see cref="ResetTenantCommand.ConfirmarNomeFantasia"/> bate com o nome real (case-insensitive).
/// 2. Valida motivo mín. 10 chars.
/// 3. Chama <see cref="IAdminResetService"/> (ResetModulos.Tudo).
/// 4. Gera audit <c>ResetarTenant</c>.
/// Scoped: depende de IAdminResetService e ImedtoAdminAuditWriter.
/// </summary>
public class ResetTenantCommandHandler : ICommandHandler<ResetTenantCommand>
{
    private readonly IAdminResetService _resetService;
    private readonly IAdminEstabelecimentosQueryRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;

    public ResetTenantCommandHandler(
        IAdminResetService resetService,
        IAdminEstabelecimentosQueryRepository repo,
        ImedtoAdminAuditWriter audit)
    {
        _resetService = resetService;
        _repo = repo;
        _audit = audit;
    }

    public async Task Handle(ResetTenantCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Motivo) || command.Motivo.Trim().Length < 10)
            throw new BusinessException("Motivo é obrigatório e deve ter ao menos 10 caracteres.");

        if (string.IsNullOrWhiteSpace(command.ConfirmarNomeFantasia))
            throw new BusinessException("Confirmação do nome fantasia é obrigatória.");

        var (_, nomeFantasia) = await _repo.ObterCpfENomeFantasiaAsync(command.EstabelecimentoId);

        if (nomeFantasia is null)
            throw new BusinessException("Estabelecimento não encontrado.");

        if (!string.Equals(command.ConfirmarNomeFantasia.Trim(), nomeFantasia.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("Nome fantasia informado não confere. Digite exatamente o nome do estabelecimento.");

        await _resetService.ResetEstabelecimentoAsync(
            command.EstabelecimentoId,
            ResetModulos.Tudo(),
            command.Motivo.Trim(),
            command.AdminId);

        // Audit obrigatório: falha aqui não precisa reverter o reset (já foi).
        // Usa RegistrarAsync (lança em falha) para garantir rastreabilidade.
        await _audit.RegistrarAsync(
            AcoesAuditAdmin.ResetarTenant,
            command.AdminId,
            "Estabelecimento",
            command.EstabelecimentoId.ToString(),
            tenantAfetadoId: command.EstabelecimentoId,
            motivo: command.Motivo.Trim());
    }
}
