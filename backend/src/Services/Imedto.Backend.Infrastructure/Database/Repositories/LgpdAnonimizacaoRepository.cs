using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Lgpd;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class LgpdAnonimizacaoRepository : ILgpdAnonimizacaoRepository
{
    private readonly AppDbContext _db;

    public LgpdAnonimizacaoRepository(AppDbContext db) => _db = db;

    public async Task Salvar(LgpdAnonimizacao registro)
    {
        _db.LgpdAnonimizacoes.Add(registro);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<LgpdAnonimizacao>> ListarPorRegistro(string tabela, long registroId) =>
        await _db.LgpdAnonimizacoes
            .AsNoTracking()
            .Where(a => a.Tabela == tabela && a.RegistroId == registroId)
            .OrderByDescending(a => a.AnonimizadoEm)
            .ToListAsync();
}
