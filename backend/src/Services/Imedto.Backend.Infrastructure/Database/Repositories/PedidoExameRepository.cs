using Imedto.Backend.Domain.PedidosExame;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class PedidoExameRepository : IPedidoExameRepository
{
    private readonly AppDbContext _context;

    public PedidoExameRepository(AppDbContext context) => _context = context;

    public async Task<PedidoExame?> ObterPorIdOuNulo(long id, long estabelecimentoId) =>
        await _context.PedidosExame
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.EstabelecimentoId == estabelecimentoId &&
                p.DeletadoEm == null);

    public async Task Salvar(PedidoExame pedido)
    {
        if (pedido.Id == 0)
        {
            await _context.PedidosExame.AddAsync(pedido);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.PedidosExame.Update(pedido);
        }
    }
}
