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
    private readonly CatalogoRegioesCacheInvalidador _cacheInvalidador;

    public InativarRegiaoAdminCommandHandler(
        RegiaoAnatomicaCatalogoRepository repo,
        RegiaoAnatomicaAdminQueryRepository query,
        ImedtoAdminAuditWriter audit,
        CatalogoRegioesCacheInvalidador cacheInvalidador)
    {
        _repo = repo;
        _query = query;
        _audit = audit;
        _cacheInvalidador = cacheInvalidador;
    }

    public async Task Handle(InativarRegiaoAdminCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Informe o motivo da alteração (mínimo 10 caracteres).");

        var regiao = await _repo.ObterPorIdOuNulo(command.Id);
        if (regiao is null)
            throw new BusinessException("Região anatômica não encontrada.");

        // R2: nível 1 (raiz) sustenta o mapa corporal — não pode ser inativado via tela de admin.
        if (regiao.Nivel == 1)
            throw new BusinessException("Regiões de nível 1 (raiz) sustentam o mapa corporal e não podem ser inativadas.");

        regiao.Inativar();
        await _repo.Salvar();

        _cacheInvalidador.InvalidarTudo();

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
