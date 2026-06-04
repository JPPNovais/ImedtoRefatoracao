using System.Text.Json;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.ModelosPermissaoPadraoSistema;

/// <summary>
/// Exclui o registro global e todas as cópias correlacionadas, bloqueando se em uso por vínculo ativo.
/// CA4, CA5, CA14 do briefing 2026-06-04_001.
/// </summary>
public class ExcluirModeloPermissaoPadraoSistemaCommandHandler
{
    private readonly IModeloPermissaoRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public ExcluirModeloPermissaoPadraoSistemaCommandHandler(
        IModeloPermissaoRepository repo,
        ImedtoAdminAuditWriter audit,
        AppDbContext db)
    {
        _repo = repo;
        _audit = audit;
        _db = db;
    }

    public async Task Handle(ExcluirModeloPermissaoPadraoSistemaCommand command, CancellationToken ct = default)
    {
        var global = await _repo.ObterGlobalPorIdOuNulo(command.Id);
        if (global is null)
            throw new BusinessException("Modelo não encontrado.");

        // R6 — bloqueio se em uso por vínculo ativo em qualquer estabelecimento
        if (await _repo.CopiaEstaEmUsoEmQualquerEstabelecimento(global.Nome, ct))
            throw new BusinessException("Não é possível excluir: há profissionais vinculados a este modelo em uma ou mais clínicas.");

        // Carregar cópias antes de excluir para o audit
        var copias = await _repo.ListarCopiasPadraoDoGlobal(global.Nome, ct);
        var nomeExcluido = global.Nome;
        var nCopias = copias.Count;

        // Excluir cópias do tenant
        foreach (var copia in copias)
            _db.ModelosPermissao.Remove(copia);

        // Excluir registro global
        _db.ModelosPermissao.Remove(global);

        await _db.SaveChangesAsync(ct);

        var payloadJson = JsonSerializer.Serialize(new
        {
            nome = nomeExcluido,
            nInstanciasExcluidas = nCopias,
        });

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.ExcluirModeloPermissaoPadraoSistema,
            command.AdminId,
            recursoTipo: "modelo_permissao_padrao",
            recursoId: command.Id.ToString(),
            payloadJson: payloadJson,
            ct: ct);
    }
}

public record ExcluirModeloPermissaoPadraoSistemaCommand(long Id, Guid? AdminId);
