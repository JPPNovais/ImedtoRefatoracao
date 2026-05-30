using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Regioes;

public class ExcluirRegiaoAdminCommandHandler
{
    private readonly RegiaoAnatomicaCatalogoRepository _repo;
    private readonly RegiaoAnatomicaAdminQueryRepository _query;
    private readonly ImedtoAdminAuditWriter _audit;

    public ExcluirRegiaoAdminCommandHandler(
        RegiaoAnatomicaCatalogoRepository repo,
        RegiaoAnatomicaAdminQueryRepository query,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _query = query;
        _audit = audit;
    }

    public async Task Handle(ExcluirRegiaoAdminCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Informe o motivo da exclusão (mínimo 10 caracteres).");

        var regiao = await _repo.ObterPorIdOuNulo(command.Id);
        if (regiao is null)
            throw new BusinessException("Região anatômica não encontrada.");

        if (await _query.TemFilhosAsync(command.Id, ct))
            throw new BusinessException("Esta região tem subgrupos. Inative em vez de excluir, ou remova os subgrupos primeiro.");

        var regiaoId = regiao.Id;

        await _repo.Excluir(regiao);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.ExcluirRegiaoAnatomica,
            command.AdminId,
            recursoTipo: "regiao_anatomica",
            recursoId: regiaoId.ToString(),
            motivo: command.Motivo,
            ct: ct);
    }
}

public record ExcluirRegiaoAdminCommand(long Id, string Motivo, Guid? AdminId);
