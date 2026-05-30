using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Catalogo;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>
/// Repositório de escrita para <see cref="RegiaoAnatomicaCatalogo"/>.
/// Usado exclusivamente pelos handlers admin (Wave 4).
/// </summary>
public class RegiaoAnatomicaCatalogoRepository
{
    private readonly AppDbContext _context;

    public RegiaoAnatomicaCatalogoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RegiaoAnatomicaCatalogo?> ObterPorIdOuNulo(long id) =>
        await _context.RegioesAnatomicasCatalogo.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<RegiaoAnatomicaCatalogo?> ObterPorCodigoOuNulo(string codigo) =>
        await _context.RegioesAnatomicasCatalogo
            .FirstOrDefaultAsync(r => r.Codigo.ToLower() == codigo.ToLower());

    public async Task Adicionar(RegiaoAnatomicaCatalogo regiao)
    {
        await _context.RegioesAnatomicasCatalogo.AddAsync(regiao);
        await _context.SaveChangesAsync();
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }

    public async Task Excluir(RegiaoAnatomicaCatalogo regiao)
    {
        _context.RegioesAnatomicasCatalogo.Remove(regiao);
        await _context.SaveChangesAsync();
    }
}
