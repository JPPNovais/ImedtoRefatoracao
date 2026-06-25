using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Regioes;

public class AtualizarRegiaoAdminCommandHandler
{
    private readonly RegiaoAnatomicaCatalogoRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly CatalogoRegioesCacheInvalidador _cacheInvalidador;

    public AtualizarRegiaoAdminCommandHandler(
        RegiaoAnatomicaCatalogoRepository repo,
        ImedtoAdminAuditWriter audit,
        CatalogoRegioesCacheInvalidador cacheInvalidador)
    {
        _repo = repo;
        _audit = audit;
        _cacheInvalidador = cacheInvalidador;
    }

    public async Task Handle(AtualizarRegiaoAdminCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Informe o motivo da alteração (mínimo 10 caracteres).");

        var regiao = await _repo.ObterPorIdOuNulo(command.Id);
        if (regiao is null)
            throw new BusinessException("Região anatômica não encontrada.");

        regiao.Atualizar(command.Nome, command.TemplateTexto, null);
        await _repo.Salvar();

        _cacheInvalidador.InvalidarTudo();

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.AtualizarRegiaoAnatomica,
            command.AdminId,
            recursoTipo: "regiao_anatomica",
            recursoId: regiao.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);
    }
}

public record AtualizarRegiaoAdminCommand(
    long Id,
    string Nome,
    string? TemplateTexto,
    string Motivo,
    Guid? AdminId);
