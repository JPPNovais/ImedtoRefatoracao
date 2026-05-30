using Imedto.Backend.Contracts.Admin.Catalogos.Variaveis.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Catalogos.Variaveis;

public class DesativarVariavelGlobalCommandHandler
{
    private readonly ImedtoVariavelPoolGlobalRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;

    public DesativarVariavelGlobalCommandHandler(
        ImedtoVariavelPoolGlobalRepository repo,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task Handle(DesativarVariavelGlobalCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Motivo deve ter ao menos 10 caracteres.");

        var variavel = await _repo.ObterPorIdAsync(command.Id, ct)
            ?? throw new BusinessException("Variável não encontrada.");

        variavel.Desativar(command.AdminId);
        await _repo.SalvarAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.DesativarVariavelGlobal,
            command.AdminId,
            recursoTipo: "variavel_pool_global",
            recursoId: command.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);
    }
}
