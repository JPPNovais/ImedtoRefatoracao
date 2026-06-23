using System.Text.Json;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.CategoriasFinanceiras;

/// <summary>
/// Cria uma nova categoria financeira padrão global e propaga (por cópia materializada) a
/// todos os estabelecimentos que ainda não possuem um categoria com o mesmo nome (R3 — idempotente).
/// CA4, CA7, CA8 do briefing 2026-06-22_003 M3.
/// </summary>
public class CriarCategoriaFinanceiraPadraoSistemaCommandHandler
{
    private readonly ICategoriaFinanceiraPadraoSistemaRepository _repo;
    private readonly ICategoriaFinanceiraRepository _categoriaRepo;
    private readonly CategoriaFinanceiraPadraoSistemaQueryRepository _query;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public CriarCategoriaFinanceiraPadraoSistemaCommandHandler(
        ICategoriaFinanceiraPadraoSistemaRepository repo,
        ICategoriaFinanceiraRepository categoriaRepo,
        CategoriaFinanceiraPadraoSistemaQueryRepository query,
        ImedtoAdminAuditWriter audit,
        AppDbContext db)
    {
        _repo = repo;
        _categoriaRepo = categoriaRepo;
        _query = query;
        _audit = audit;
        _db = db;
    }

    public async Task<long> Handle(CriarCategoriaFinanceiraPadraoSistemaCommand command, CancellationToken ct = default)
    {
        // Unicidade (nome + tipo) no catálogo global
        if (await _repo.ExisteGlobalComNomeETipo(command.Nome, command.Tipo, ct))
            throw new BusinessException("Já existe uma categoria padrão com este nome e tipo.");

        var global = CategoriaFinanceiraPadraoSistema.Criar(command.Nome, command.Tipo);

        // INSERT do global — Salvar faz SaveChangesAsync para popular o Id
        await _repo.Salvar(global, ct);

        // R3 — propagação retroativa aos estabelecimentos que ainda não possuem o nome
        var todosEstabelecimentos = await _query.ListarIdsEstabelecimentos(ct);
        var estabelecimentosComNome = await _query.ListarEstabelecimentosComNome(command.Nome, ct);
        var jaTemNome = estabelecimentosComNome.ToHashSet();

        var propagados = 0;
        foreach (var estabId in todosEstabelecimentos)
        {
            if (jaTemNome.Contains(estabId)) continue; // idempotente: pula quem já tem o nome
            _db.CategoriasFinanceiras.Add(CategoriaFinanceira.CriarPadrao(estabId, global.Nome, global.Tipo));
            propagados++;
        }

        await _db.SaveChangesAsync(ct);

        var payloadJson = JsonSerializer.Serialize(new
        {
            nome = global.Nome,
            tipo = global.Tipo.ToString(),
            nInstanciasPropagadas = propagados,
        });

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.CriarCategoriaFinanceiraPadraoSistema,
            command.AdminId,
            recursoTipo: "categoria_financeira_padrao",
            recursoId: global.Id.ToString(),
            payloadJson: payloadJson,
            ct: ct);

        return global.Id;
    }
}

public record CriarCategoriaFinanceiraPadraoSistemaCommand(
    string Nome,
    TipoCategoria Tipo,
    Guid? AdminId);
