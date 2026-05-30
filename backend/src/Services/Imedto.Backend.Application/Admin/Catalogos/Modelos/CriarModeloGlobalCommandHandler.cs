using Imedto.Backend.Contracts.Admin.Catalogos.Modelos.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Catalogos.Modelos;

public class CriarModeloGlobalCommandHandler
{
    private readonly ImedtoModeloProntuarioGlobalRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;

    public CriarModeloGlobalCommandHandler(
        ImedtoModeloProntuarioGlobalRepository repo,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<Guid> Handle(CriarModeloGlobalCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Motivo deve ter ao menos 10 caracteres.");

        var modelo = ImedtoModeloProntuarioGlobal.Criar(
            command.Nome,
            command.Descricao,
            command.ConteudoJson,
            command.AdminId);

        _repo.Adicionar(modelo);
        await _repo.SalvarAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.CriarModeloGlobal,
            command.AdminId,
            recursoTipo: "modelo_prontuario_global",
            recursoId: modelo.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);

        return modelo.Id;
    }
}
