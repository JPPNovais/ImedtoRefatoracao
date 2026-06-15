using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Repositório EF para <see cref="MigracaoTemplate"/>.
/// Templates são cross-tenant — sem filtro por estabelecimento_id.
/// </summary>
public class MigracaoTemplateRepository : IMigracaoTemplateRepository
{
    private readonly AppDbContext _db;

    public MigracaoTemplateRepository(AppDbContext db) => _db = db;

    public async Task Salvar(MigracaoTemplate template, CancellationToken ct = default)
    {
        if (template.Id == 0)
            _db.MigracaoTemplates.Add(template);
        else
            _db.MigracaoTemplates.Update(template);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<MigracaoTemplate?> ObterPorNomeEEntidadeOuNulo(
        string nome,
        string entidade,
        CancellationToken ct = default)
    {
        return await _db.MigracaoTemplates
            .FirstOrDefaultAsync(t => t.Nome == nome && t.Entidade == entidade, ct);
    }

    public async Task<List<MigracaoTemplate>> ListarPorNome(string nome, CancellationToken ct = default)
    {
        return await _db.MigracaoTemplates
            .Where(t => t.Nome == nome)
            .OrderBy(t => t.Entidade)
            .ToListAsync(ct);
    }

    public async Task<List<MigracaoTemplate>> Listar(int pagina, int tamanho, CancellationToken ct = default)
    {
        return await _db.MigracaoTemplates
            .OrderBy(t => t.Nome)
            .ThenBy(t => t.Entidade)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(ct);
    }

    public async Task<int> ContarTotal(CancellationToken ct = default)
    {
        return await _db.MigracaoTemplates.CountAsync(ct);
    }
}
