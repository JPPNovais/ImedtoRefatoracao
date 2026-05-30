using Imedto.Backend.Contracts.Admin.Catalogos.Variaveis.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Catalogos.Variaveis;

public class CriarVariavelGlobalCommandHandler
{
    private readonly ImedtoVariavelPoolGlobalRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;

    public CriarVariavelGlobalCommandHandler(
        ImedtoVariavelPoolGlobalRepository repo,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<Guid> Handle(CriarVariavelGlobalCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Motivo deve ter ao menos 10 caracteres.");

        var variavel = ImedtoVariavelPoolGlobal.Criar(
            command.Nome,
            command.Tipo,
            command.ValoresJson,
            command.Descricao,
            command.AdminId);

        _repo.Adicionar(variavel);
        await _repo.SalvarAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.CriarVariavelGlobal,
            command.AdminId,
            recursoTipo: "variavel_pool_global",
            recursoId: variavel.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);

        return variavel.Id;
    }
}
