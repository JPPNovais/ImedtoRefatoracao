using Imedto.Backend.Domain.Prontuarios;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ExameFisicoRepository : IExameFisicoRepository
{
    private readonly AppDbContext _context;

    public ExameFisicoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ExameFisico> ObterPorId(long id)
    {
        var e = await _context.ExamesFisicos
            .Include(x => x.Regioes)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) throw new KeyNotFoundException($"Exame físico {id} não encontrado.");
        return e;
    }

    public async Task<ExameFisico?> ObterPorIdOuNulo(long id)
    {
        return await _context.ExamesFisicos
            .Include(x => x.Regioes)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task Salvar(ExameFisico exame)
    {
        if (exame.Id == 0)
        {
            await _context.ExamesFisicos.AddAsync(exame);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.ExamesFisicos.Update(exame);
        }
    }
}
