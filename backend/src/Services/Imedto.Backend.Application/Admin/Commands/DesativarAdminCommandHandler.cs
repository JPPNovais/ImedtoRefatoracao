using Imedto.Backend.Contracts.Admin.Admins.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Admins.Commands;

public class DesativarAdminCommandHandler
{
    private readonly ImedtoAdminRepository _repo;
    private readonly ImedtoAdminRefreshTokenRepository _refreshRepo;
    private readonly ImedtoAdminAuditWriter _audit;

    public DesativarAdminCommandHandler(
        ImedtoAdminRepository repo,
        ImedtoAdminRefreshTokenRepository refreshRepo,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _refreshRepo = refreshRepo;
        _audit = audit;
    }

    public async Task Handle(DesativarAdminCommand command, CancellationToken ct = default)
    {
        ValidarMotivo(command.Motivo);

        var alvo = await _repo.ObterPorIdAsync(command.AdminAlvoId, ct)
            ?? throw new BusinessException("Administrador não encontrado.");

        var totalAtivos = await _repo.ContarAtivosAsync(ct);

        if (totalAtivos <= 1 && alvo.Ativo)
            throw new BusinessException("Não é possível desativar o último administrador ativo do sistema.");

        alvo.Desativar(command.AdminSolicitanteId);
        _repo.Atualizar(alvo);

        await _refreshRepo.RevogarTodosDoAdminAsync(command.AdminAlvoId, ct);

        await _audit.RegistrarAsync(
            acao: AcoesAuditAdmin.DesativarAdmin,
            adminId: command.AdminSolicitanteId,
            recursoTipo: "admin",
            recursoId: command.AdminAlvoId.ToString(),
            motivo: command.Motivo,
            ct: ct);
    }

    private static void ValidarMotivo(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 10)
            throw new BusinessException("Motivo é obrigatório para esta operação (mínimo 10 caracteres).");
        if (motivo.Length > 500)
            throw new BusinessException("Motivo não pode exceder 500 caracteres.");
    }
}
