using System.Text.Json;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.ModelosPermissaoPadraoSistema;

/// <summary>
/// Cria um novo modelo de permissão padrão do sistema (registro global + cópias em cada estabelecimento).
/// CA1, CA13, CA14 do briefing 2026-06-04_001.
/// </summary>
public class CriarModeloPermissaoPadraoSistemaCommandHandler
{
    private readonly IModeloPermissaoRepository _repo;
    private readonly ModeloPermissaoPadraoSistemaQueryRepository _query;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public CriarModeloPermissaoPadraoSistemaCommandHandler(
        IModeloPermissaoRepository repo,
        ModeloPermissaoPadraoSistemaQueryRepository query,
        ImedtoAdminAuditWriter audit,
        AppDbContext db)
    {
        _repo = repo;
        _query = query;
        _audit = audit;
        _db = db;
    }

    public async Task<long> Handle(CriarModeloPermissaoPadraoSistemaCommand command, CancellationToken ct = default)
    {
        // R9a — unicidade no escopo global
        if (await _repo.ExisteGlobalComNome(command.Nome, ct: ct))
            throw new BusinessException("Já existe um modelo padrão do sistema com esse nome.");

        // R9b — colisão com cópias de tenant inviabilizaria o INSERT das cópias
        if (await _repo.ExisteNomeEmQualquerEstabelecimento(command.Nome, ct: ct))
            throw new BusinessException("Já existe um modelo com esse nome em uma ou mais clínicas; escolha outro nome.");

        var global = ModeloPermissaoEstabelecimento.CriarGlobal(
            command.Nome,
            command.TipoAcesso,
            command.Permissoes,
            command.PermissoesExtras,
            command.Icone,
            command.Cor,
            command.Descricao);

        // INSERT do global — o Salvar faz SaveChangesAsync interno para popular o Id
        await _repo.Salvar(global);

        // Propagação retroativa — materializa cópia em cada estabelecimento existente (R4, R11)
        var idsEstabelecimentos = await _query.ListarIdsEstabelecimentos(ct);
        foreach (var estabId in idsEstabelecimentos)
        {
            var copia = ModeloPermissaoEstabelecimento.CriarCopiaDeGlobal(global, estabId);
            _db.ModelosPermissao.Add(copia);
        }

        await _db.SaveChangesAsync(ct);

        var payloadJson = JsonSerializer.Serialize(new
        {
            nome = global.Nome,
            tipoAcesso = global.TipoAcesso.ToString(),
            nInstanciasPropagadas = idsEstabelecimentos.Count,
        });

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.CriarModeloPermissaoPadraoSistema,
            command.AdminId,
            recursoTipo: "modelo_permissao_padrao",
            recursoId: global.Id.ToString(),
            payloadJson: payloadJson,
            ct: ct);

        return global.Id;
    }
}

public record CriarModeloPermissaoPadraoSistemaCommand(
    string Nome,
    TipoAcessoModelo TipoAcesso,
    IReadOnlyList<string>? Permissoes,
    IReadOnlyList<string>? PermissoesExtras,
    string? Icone,
    string? Cor,
    string? Descricao,
    Guid? AdminId);
