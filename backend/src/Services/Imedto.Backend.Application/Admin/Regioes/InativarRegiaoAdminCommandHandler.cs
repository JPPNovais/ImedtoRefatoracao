using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Regioes;

public class InativarRegiaoAdminCommandHandler
{
    private readonly RegiaoAnatomicaCatalogoRepository _repo;
    private readonly RegiaoAnatomicaAdminQueryRepository _query;
    private readonly ImedtoAdminAuditWriter _audit;

    public InativarRegiaoAdminCommandHandler(
        RegiaoAnatomicaCatalogoRepository repo,
        RegiaoAnatomicaAdminQueryRepository query,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _query = query;
        _audit = audit;
    }

    public async Task Handle(InativarRegiaoAdminCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Informe o motivo da alteração (mínimo 10 caracteres).");

        var regiao = await _repo.ObterPorIdOuNulo(command.Id);
        if (regiao is null)
            throw new BusinessException("Região anatômica não encontrada.");

        regiao.Inativar();
        await _repo.Salvar();

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.InativarRegiaoAnatomica,
            command.AdminId,
            recursoTipo: "regiao_anatomica",
            recursoId: regiao.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);
    }
}

public record InativarRegiaoAdminCommand(long Id, string Motivo, Guid? AdminId);
