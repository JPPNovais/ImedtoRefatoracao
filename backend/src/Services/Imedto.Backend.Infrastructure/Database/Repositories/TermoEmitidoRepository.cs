using Imedto.Backend.Domain.Termos;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public sealed class TermoEmitidoRepository : ITermoEmitidoRepository
{
    private readonly AppDbContext _context;

    public TermoEmitidoRepository(AppDbContext context) => _context = context;

    public Task<TermoEmitido> ObterPorIdOuNulo(long id, long estabelecimentoId) =>
        _context.TermosEmitidos
            .FirstOrDefaultAsync(t => t.Id == id && t.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(TermoEmitido termo)
    {
        if (termo.Id == 0)
        {
            await _context.TermosEmitidos.AddAsync(termo);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.TermosEmitidos.Update(termo);
        }
    }
}
