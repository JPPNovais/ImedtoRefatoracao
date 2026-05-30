using Imedto.Backend.Contracts.Admin.Admins.Commands;
using Imedto.Backend.Contracts.Admin.Admins.Queries.Results;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Admins.Commands;

public class ResetSenhaAdminCommandHandler
{
    private readonly ImedtoAdminRepository _repo;
    private readonly ImedtoAdminRefreshTokenRepository _refreshRepo;
    private readonly IPasswordHasher _hasher;
    private readonly ImedtoAdminAuditWriter _audit;

    public ResetSenhaAdminCommandHandler(
        ImedtoAdminRepository repo,
        ImedtoAdminRefreshTokenRepository refreshRepo,
        IPasswordHasher hasher,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _refreshRepo = refreshRepo;
        _hasher = hasher;
        _audit = audit;
    }

    public async Task<SenhaResetadaResult> Handle(ResetSenhaAdminCommand command, CancellationToken ct = default)
    {
        ValidarMotivo(command.Motivo);

        if (command.AdminSolicitanteId == command.AdminAlvoId)
            throw new BusinessException("Use o endpoint de redefinição de senha para alterar sua própria senha.");

        var alvo = await _repo.ObterPorIdAsync(command.AdminAlvoId, ct)
            ?? throw new BusinessException("Administrador não encontrado.");

        var senhaTemp = AdminSenhaPolicy.GerarSenhaTemporaria();
        var novoHash = _hasher.Hash(senhaTemp);

        alvo.AtualizarSenha(novoHash, forceReset: true);
        _repo.Atualizar(alvo);

        await _refreshRepo.RevogarTodosDoAdminAsync(command.AdminAlvoId, ct);

        await _audit.RegistrarAsync(
            acao: AcoesAuditAdmin.ResetarSenhaAdmin,
            adminId: command.AdminSolicitanteId,
            recursoTipo: "admin",
            recursoId: command.AdminAlvoId.ToString(),
            motivo: command.Motivo,
            ct: ct);

        return new SenhaResetadaResult(senhaTemp);
    }

    private static void ValidarMotivo(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 10)
            throw new BusinessException("Motivo é obrigatório para esta operação (mínimo 10 caracteres).");
        if (motivo.Length > 500)
            throw new BusinessException("Motivo não pode exceder 500 caracteres.");
    }
}
