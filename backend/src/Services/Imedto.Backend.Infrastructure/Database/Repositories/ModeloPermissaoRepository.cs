using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.ModelosPermissao;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ModeloPermissaoRepository : IModeloPermissaoRepository
{
    private readonly AppDbContext _context;

    public ModeloPermissaoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ModeloPermissaoEstabelecimento> ObterPorId(long id)
    {
        var modelo = await _context.ModelosPermissao.FindAsync(id);
        if (modelo is null)
            throw new KeyNotFoundException($"Modelo de permissão {id} não encontrado.");
        return modelo;
    }

    public async Task<ModeloPermissaoEstabelecimento?> ObterPorIdOuNulo(long id) =>
        await _context.ModelosPermissao.FindAsync(id);

    public async Task<ModeloPermissaoEstabelecimento> ObterPadraoDoEstabelecimento(long estabelecimentoId) =>
        await _context.ModelosPermissao
            .FirstOrDefaultAsync(m => m.EstabelecimentoId == estabelecimentoId && m.EhPadrao);

    public async Task<bool> PertenceAoEstabelecimento(long modeloId, long estabelecimentoId) =>
        await _context.ModelosPermissao
            .AsNoTracking()
            .AnyAsync(m => m.Id == modeloId && m.EstabelecimentoId == estabelecimentoId);

    public async Task<bool> EstaEmUsoPorVinculoAtivo(long modeloId) =>
        await _context.Vinculos
            .AsNoTracking()
            .AnyAsync(v => v.ModeloPermissaoId == modeloId
                        && v.Status != Domain.Vinculos.VinculoStatus.Inativo);

    public async Task<bool> ExisteComNomeNoEstabelecimento(string nome, long estabelecimentoId, long? excetoId = null)
    {
        if (string.IsNullOrWhiteSpace(nome)) return false;
        var nomeNorm = nome.Trim();
        var q = _context.ModelosPermissao
            .AsNoTracking()
            .Where(m => m.EstabelecimentoId == estabelecimentoId && m.Nome == nomeNorm);
        if (excetoId is { } id) q = q.Where(m => m.Id != id);
        return await q.AnyAsync();
    }

    // ─── Métodos Admin Global ──────────────────────────────────────────────────

    public async Task<ModeloPermissaoEstabelecimento?> ObterGlobalPorIdOuNulo(long id) =>
        await _context.ModelosPermissao
            .FirstOrDefaultAsync(m => m.Id == id && m.EstabelecimentoId == null);

    public async Task<IReadOnlyList<ModeloPermissaoEstabelecimento>> ListarGlobais() =>
        await _context.ModelosPermissao
            .Where(m => m.EstabelecimentoId == null)
            .OrderBy(m => m.Nome)
            .ToListAsync();

    public async Task<bool> ExisteGlobalComNome(string nome, long? excetoId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nome)) return false;
        var nomeNorm = nome.Trim();
        var q = _context.ModelosPermissao
            .AsNoTracking()
            .Where(m => m.EstabelecimentoId == null && m.Nome == nomeNorm);
        if (excetoId is { } id) q = q.Where(m => m.Id != id);
        return await q.AnyAsync(ct);
    }

    public async Task<bool> ExisteNomeEmQualquerEstabelecimento(string nome, long? excetoIdGlobal = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nome)) return false;
        var nomeNorm = nome.Trim();
        return await _context.ModelosPermissao
            .AsNoTracking()
            .AnyAsync(m => m.EstabelecimentoId != null && m.Nome == nomeNorm, ct);
    }

    public async Task<IReadOnlyList<ModeloPermissaoEstabelecimento>> ListarCopiasPadraoDoGlobal(string nomeGlobal, CancellationToken ct = default)
    {
        var nomeNorm = nomeGlobal.Trim();
        return await _context.ModelosPermissao
            .Where(m => m.EstabelecimentoId != null && m.EhPadrao && m.Nome == nomeNorm)
            .ToListAsync(ct);
    }

    public async Task<bool> CopiaEstaEmUsoEmQualquerEstabelecimento(string nomeGlobal, CancellationToken ct = default)
    {
        var nomeNorm = nomeGlobal.Trim();
        return await _context.Vinculos
            .AsNoTracking()
            .AnyAsync(v =>
                v.Status != Domain.Vinculos.VinculoStatus.Inativo
                && _context.ModelosPermissao
                    .Any(m => m.Id == v.ModeloPermissaoId
                              && m.EstabelecimentoId != null
                              && m.EhPadrao
                              && m.Nome == nomeNorm),
                ct);
    }

    public async Task<int> ContarEstabelecimentos(CancellationToken ct = default) =>
        await _context.Estabelecimentos.CountAsync(ct);

    public async Task Salvar(ModeloPermissaoEstabelecimento modelo)
    {
        if (modelo.Id == 0)
        {
            await _context.ModelosPermissao.AddAsync(modelo);
            // SaveChanges aqui pra popular o Id (igual OrcamentoRepository/VinculoRepository).
            // Sem isso o handler retorna { modeloId: 0 } no Created — o segundo SaveChanges
            // do UnitOfWorkFilter é no-op porque não há mudanças pendentes.
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.ModelosPermissao.Update(modelo);
        }
    }

    public Task Excluir(ModeloPermissaoEstabelecimento modelo)
    {
        _context.ModelosPermissao.Remove(modelo);
        return Task.CompletedTask;
    }

    public async Task<bool> UsuarioTemPermissaoExtra(
        Guid usuarioId,
        long estabelecimentoId,
        string permissao,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(permissao)) return false;

        // Dono do estabelecimento sempre tem todas as permissões finas — replica a
        // regra unificada de IVinculoRepository.PodeAtuarComoProfissional.
        var ehDono = await _context.Estabelecimentos
            .AsNoTracking()
            .AnyAsync(e => e.Id == estabelecimentoId && e.DonoUsuarioId == usuarioId, ct);
        if (ehDono) return true;

        // Vínculo ativo cujo modelo de permissão contenha a chave em `permissoes_extras`.
        // Operador `@>` do Postgres resolve "jsonb contém este array" de forma performática.
        // FormattableString → `SqlQuery` parametriza cada interpolação como parâmetro Npgsql
        // (não há injection: `permissaoTrim` nunca entra no SQL como literal).
        var permissaoTrim = permissao.Trim();
        FormattableString sql = $"""
            SELECT EXISTS (
                SELECT 1
                FROM   public.vinculo_profissional_estabelecimento v
                JOIN   public.modelo_permissao_estabelecimento mp ON mp.id = v.modelo_permissao_id
                WHERE  v.profissional_usuario_id = {usuarioId}
                  AND  v.estabelecimento_id      = {estabelecimentoId}
                  AND  v.status                  = 'Ativo'
                  AND  mp.permissoes_extras @> jsonb_build_array({permissaoTrim}::text)
            ) AS "Value"
            """;

        return await _context.Database
            .SqlQuery<bool>(sql)
            .FirstAsync(ct);
    }

    public async Task<bool> UsuarioTemAcao(
        Guid usuarioId,
        long estabelecimentoId,
        string area,
        string? acao = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(area)) return false;

        var ehDono = await _context.Estabelecimentos
            .AsNoTracking()
            .AnyAsync(e => e.Id == estabelecimentoId && e.DonoUsuarioId == usuarioId, ct);
        if (ehDono) return true;

        var areaTrim = area.Trim();
        var chaveGranular = string.IsNullOrWhiteSpace(acao) ? null : $"{areaTrim}.{acao.Trim()}";

        // Profissional: vínculo ativo cujo modelo tem a chave granular OU a chave legada da área.
        // Quando `acao` é nula/vazia, qualquer chave que comece com a área (legado) basta.
        FormattableString sql;
        if (chaveGranular is null)
        {
            var prefixoLike = areaTrim + ".%";
            sql = $"""
                SELECT EXISTS (
                    SELECT 1
                    FROM   public.vinculo_profissional_estabelecimento v
                    JOIN   public.modelo_permissao_estabelecimento mp ON mp.id = v.modelo_permissao_id
                    WHERE  v.profissional_usuario_id = {usuarioId}
                      AND  v.estabelecimento_id      = {estabelecimentoId}
                      AND  v.status                  = 'Ativo'
                      AND  (
                            mp.permissoes @> jsonb_build_array({areaTrim}::text)
                         OR EXISTS (
                                SELECT 1 FROM jsonb_array_elements_text(mp.permissoes) AS p(val)
                                WHERE p.val LIKE {prefixoLike}
                            )
                      )
                ) AS "Value"
                """;
        }
        else
        {
            sql = $"""
                SELECT EXISTS (
                    SELECT 1
                    FROM   public.vinculo_profissional_estabelecimento v
                    JOIN   public.modelo_permissao_estabelecimento mp ON mp.id = v.modelo_permissao_id
                    WHERE  v.profissional_usuario_id = {usuarioId}
                      AND  v.estabelecimento_id      = {estabelecimentoId}
                      AND  v.status                  = 'Ativo'
                      AND  (
                            mp.permissoes @> jsonb_build_array({chaveGranular}::text)
                         OR mp.permissoes @> jsonb_build_array({areaTrim}::text)
                      )
                ) AS "Value"
                """;
        }

        return await _context.Database
            .SqlQuery<bool>(sql)
            .FirstAsync(ct);
    }
}
