using Imedto.Backend.Contracts.Admin.Admins.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Admins.Commands;

public class ReativarAdminCommandHandler
{
    private readonly ImedtoAdminRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;

    public ReativarAdminCommandHandler(
        ImedtoAdminRepository repo,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task Handle(ReativarAdminCommand command, CancellationToken ct = default)
    {
        ValidarMotivo(command.Motivo);

        var alvo = await _repo.ObterPorIdAsync(command.AdminAlvoId, ct)
            ?? throw new BusinessException("Administrador não encontrado.");

        alvo.Reativar(command.AdminSolicitanteId);
        _repo.Atualizar(alvo);

        await _audit.RegistrarAsync(
            acao: AcoesAuditAdmin.ReativarAdmin,
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
