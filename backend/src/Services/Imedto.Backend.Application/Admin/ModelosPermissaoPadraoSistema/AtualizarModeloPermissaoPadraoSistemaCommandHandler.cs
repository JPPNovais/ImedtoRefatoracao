using System.Text.Json;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.ModelosPermissaoPadraoSistema;

file static class PermissoesSerializer
{
    internal static string Serializar(IReadOnlyList<string>? permissoes)
    {
        if (permissoes is null || permissoes.Count == 0) return "[]";
        var distintos = permissoes
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct()
            .ToList();
        return JsonSerializer.Serialize(distintos);
    }
}

/// <summary>
/// Edita o registro global e propaga as alterações para todas as cópias eh_padrao=true
/// correlacionadas por Nome em todos os estabelecimentos (em transação única).
/// CA2, CA3, CA5 (via R6), CA11, CA14, CA18 do briefing 2026-06-04_001.
/// </summary>
public class AtualizarModeloPermissaoPadraoSistemaCommandHandler
{
    private readonly IModeloPermissaoRepository _repo;
    private readonly ModeloPermissaoPadraoSistemaQueryRepository _query;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public AtualizarModeloPermissaoPadraoSistemaCommandHandler(
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

    public async Task Handle(AtualizarModeloPermissaoPadraoSistemaCommand command, CancellationToken ct = default)
    {
        var global = await _repo.ObterGlobalPorIdOuNulo(command.Id);
        if (global is null)
            throw new BusinessException("Modelo não encontrado.");

        // Unicidade de nome (exceto o próprio registro)
        if (await _repo.ExisteGlobalComNome(command.Nome, excetoId: command.Id, ct: ct))
            throw new BusinessException("Já existe um modelo padrão do sistema com esse nome.");

        var nomeAnterior = global.Nome;

        // Captura antes/depois para o audit (R8 — extras não mudam)
        var antes = new { nome = global.Nome, permissoes = global.Permissoes };

        // Serializa as permissões para JSON (o domínio espera o JSON já pronto em SincronizarComGlobal)
        var permissoesJson = PermissoesSerializer.Serializar(command.Permissoes);

        // Atualizar global via SincronizarComGlobal (não passa pelo guard EhPadrao de Atualizar)
        global.SincronizarComGlobal(
            command.Nome,
            command.TipoAcesso,
            permissoesJson,
            command.Icone,
            command.Cor,
            command.Descricao);

        _db.ModelosPermissao.Update(global);

        // Propagar para todas as cópias correlacionadas pelo Nome anterior (R3, R5)
        var copias = await _repo.ListarCopiasPadraoDoGlobal(nomeAnterior, ct);
        foreach (var copia in copias)
        {
            copia.SincronizarComGlobal(
                command.Nome,
                command.TipoAcesso,
                permissoesJson,
                command.Icone,
                command.Cor,
                command.Descricao);
            _db.ModelosPermissao.Update(copia);
        }

        await _db.SaveChangesAsync(ct);

        var payloadJson = JsonSerializer.Serialize(new
        {
            antes,
            depois = new { nome = command.Nome, permissoes = command.Permissoes },
            nInstanciasPropagadas = copias.Count,
        });

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.AtualizarModeloPermissaoPadraoSistema,
            command.AdminId,
            recursoTipo: "modelo_permissao_padrao",
            recursoId: global.Id.ToString(),
            payloadJson: payloadJson,
            ct: ct);
    }
}

public record AtualizarModeloPermissaoPadraoSistemaCommand(
    long Id,
    string Nome,
    TipoAcessoModelo TipoAcesso,
    IReadOnlyList<string>? Permissoes,
    string? Icone,
    string? Cor,
    string? Descricao,
    Guid? AdminId);
