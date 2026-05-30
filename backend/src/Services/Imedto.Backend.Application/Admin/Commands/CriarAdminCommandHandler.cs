using Imedto.Backend.Contracts.Admin.Admins.Commands;
using Imedto.Backend.Contracts.Admin.Admins.Queries.Results;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Admins.Commands;

public class CriarAdminCommandHandler
{
    private readonly ImedtoAdminRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly ImedtoAdminAuditWriter _audit;

    public CriarAdminCommandHandler(
        ImedtoAdminRepository repo,
        IPasswordHasher hasher,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _hasher = hasher;
        _audit = audit;
    }

    public async Task<AdminCriadoResult> Handle(CriarAdminCommand command, CancellationToken ct = default)
    {
        ValidarMotivo(command.Motivo);

        if (string.IsNullOrWhiteSpace(command.Email))
            throw new BusinessException("E-mail é obrigatório.");
        if (string.IsNullOrWhiteSpace(command.Nome))
            throw new BusinessException("Nome é obrigatório.");

        if (await _repo.ExisteEmailAsync(command.Email.Trim().ToLowerInvariant(), ct))
            throw new BusinessException("Já existe um admin com este e-mail.");

        var senhaTemp = AdminSenhaPolicy.GerarSenhaTemporaria();
        var hash = _hasher.Hash(senhaTemp);

        var admin = ImedtoAdmin.Criar(
            email: command.Email,
            nome: command.Nome,
            senhaHash: hash,
            forcePasswordReset: true,
            criadoPorAdminId: command.AdminSolicitanteId);

        _repo.Adicionar(admin);

        await _audit.RegistrarAsync(
            acao: AcoesAuditAdmin.CriarAdmin,
            adminId: command.AdminSolicitanteId,
            recursoTipo: "admin",
            recursoId: admin.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);

        return new AdminCriadoResult(admin.Id, admin.Email, admin.Nome, senhaTemp);
    }

    private static void ValidarMotivo(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 10)
            throw new BusinessException("Motivo é obrigatório para esta operação (mínimo 10 caracteres).");
        if (motivo.Length > 500)
            throw new BusinessException("Motivo não pode exceder 500 caracteres.");
    }
}
