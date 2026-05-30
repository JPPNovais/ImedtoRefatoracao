using Imedto.Backend.Contracts.Admin.Catalogos.Variaveis.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Catalogos.Variaveis;

public class ReativarVariavelGlobalCommandHandler
{
    private readonly ImedtoVariavelPoolGlobalRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;

    public ReativarVariavelGlobalCommandHandler(
        ImedtoVariavelPoolGlobalRepository repo,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task Handle(ReativarVariavelGlobalCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Motivo deve ter ao menos 10 caracteres.");

        var variavel = await _repo.ObterPorIdAsync(command.Id, ct)
            ?? throw new BusinessException("Variável não encontrada.");

        variavel.Reativar(command.AdminId);
        await _repo.SalvarAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.ReativarVariavelGlobal,
            command.AdminId,
            recursoTipo: "variavel_pool_global",
            recursoId: command.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);
    }
}
