using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ProntuarioRepository : IProntuarioRepository
{
    private readonly AppDbContext _context;

    public ProntuarioRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Prontuario?> ObterPorIdOuNulo(long id, long estabelecimentoId) =>
        await _context.Prontuarios
            .FirstOrDefaultAsync(p => p.Id == id && p.EstabelecimentoId == estabelecimentoId);

    public async Task<Prontuario> ObterPorPaciente(long pacienteId, long estabelecimentoId) =>
        await _context.Prontuarios
            .FirstOrDefaultAsync(p =>
                p.PacienteId == pacienteId &&
                p.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(Prontuario prontuario)
    {
        if (prontuario.Id == 0)
        {
            await _context.Prontuarios.AddAsync(prontuario);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.Prontuarios.Update(prontuario);
        }
    }
}

public class ProntuarioEvolucaoRepository : IProntuarioEvolucaoRepository
{
    private readonly AppDbContext _context;

    public ProntuarioEvolucaoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task Salvar(ProntuarioEvolucao evolucao)
    {
        if (evolucao.Id == 0)
        {
            await _context.ProntuarioEvolucoes.AddAsync(evolucao);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Evoluções são append-only — update raro, só por UoW de audit.
            _context.ProntuarioEvolucoes.Update(evolucao);
        }
    }
}
