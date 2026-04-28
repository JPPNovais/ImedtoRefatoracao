using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ModeloDeProntuarioRepository : IModeloDeProntuarioRepository
{
    private readonly AppDbContext _context;

    public ModeloDeProntuarioRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ModeloDeProntuario> ObterPorId(long id)
    {
        var m = await _context.ModelosDeProntuario.FindAsync(id);
        if (m is null) throw new KeyNotFoundException($"Modelo de prontuário {id} não encontrado.");
        return m;
    }

    public async Task<ModeloDeProntuario> ObterPorIdOuNulo(long id) =>
        await _context.ModelosDeProntuario.FindAsync(id);

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
