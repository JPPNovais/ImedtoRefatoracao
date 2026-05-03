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

    public async Task Salvar(ModeloPermissaoEstabelecimento modelo)
    {
        if (modelo.Id == 0)
            await _context.ModelosPermissao.AddAsync(modelo);
        else
            _context.ModelosPermissao.Update(modelo);
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
}
