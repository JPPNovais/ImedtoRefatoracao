using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ModeloDeProntuarioRepository : IModeloDeProntuarioRepository
{
    private readonly AppDbContext _context;

    public ModeloDeProntuarioRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ModeloDeProntuario?> ObterVisivelOuNulo(long id, long estabelecimentoId) =>
        await _context.ModelosDeProntuario
            .FirstOrDefaultAsync(m => m.Id == id
                                   && (m.EhPadraoSistema || m.EstabelecimentoId == estabelecimentoId));

    public async Task Salvar(ModeloDeProntuario modelo)
    {
        if (modelo.Id == 0)
        {
            await _context.ModelosDeProntuario.AddAsync(modelo);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.ModelosDeProntuario.Update(modelo);
        }
    }

    public Task Excluir(ModeloDeProntuario modelo)
    {
        _context.ModelosDeProntuario.Remove(modelo);
        return Task.CompletedTask;
    }
}
