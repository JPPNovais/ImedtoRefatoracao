using Imedto.Backend.Contracts.Admin.Catalogos.Regioes.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Catalogos.Regioes;

public class CriarRegiaoGlobalCommandHandler
{
    private readonly ImedtoRegiaoAnatomicaGlobalRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;

    public CriarRegiaoGlobalCommandHandler(
        ImedtoRegiaoAnatomicaGlobalRepository repo,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<Guid> Handle(CriarRegiaoGlobalCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Motivo deve ter ao menos 10 caracteres.");

        var regiao = ImedtoRegiaoAnatomicaGlobal.Criar(
            command.Nome,
            command.Sinonimos,
            command.SistemaCorporal);

        _repo.Adicionar(regiao);
        await _repo.SalvarAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.CriarRegiaoGlobal,
            command.AdminId,
            recursoTipo: "regiao_anatomica_global",
            recursoId: regiao.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);

        return regiao.Id;
    }
}
