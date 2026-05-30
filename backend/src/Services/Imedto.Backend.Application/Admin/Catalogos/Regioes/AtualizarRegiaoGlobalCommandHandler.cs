using Imedto.Backend.Contracts.Admin.Catalogos.Regioes.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Catalogos.Regioes;

public class AtualizarRegiaoGlobalCommandHandler
{
    private readonly ImedtoRegiaoAnatomicaGlobalRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;

    public AtualizarRegiaoGlobalCommandHandler(
        ImedtoRegiaoAnatomicaGlobalRepository repo,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task Handle(AtualizarRegiaoGlobalCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Motivo deve ter ao menos 10 caracteres.");

        var regiao = await _repo.ObterPorIdAsync(command.Id, ct)
            ?? throw new BusinessException("Região anatômica não encontrada.");

        regiao.Atualizar(command.Nome, command.Sinonimos, command.SistemaCorporal);
        await _repo.SalvarAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.AtualizarRegiaoGlobal,
            command.AdminId,
            recursoTipo: "regiao_anatomica_global",
            recursoId: command.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);
    }
}
