using Imedto.Backend.Contracts.Admin.Catalogos.Modelos.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Catalogos.Modelos;

public class ReativarModeloGlobalCommandHandler
{
    private readonly ImedtoModeloProntuarioGlobalRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;

    public ReativarModeloGlobalCommandHandler(
        ImedtoModeloProntuarioGlobalRepository repo,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task Handle(ReativarModeloGlobalCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Motivo deve ter ao menos 10 caracteres.");

        var modelo = await _repo.ObterPorIdAsync(command.Id, ct)
            ?? throw new BusinessException("Modelo não encontrado.");

        modelo.Reativar(command.AdminId);
        await _repo.SalvarAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.ReativarModeloGlobal,
            command.AdminId,
            recursoTipo: "modelo_prontuario_global",
            recursoId: command.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);
    }
}
