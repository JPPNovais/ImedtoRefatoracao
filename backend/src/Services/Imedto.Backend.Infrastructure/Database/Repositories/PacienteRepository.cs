using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Pacientes;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class PacienteRepository : IPacienteRepository
{
    private readonly AppDbContext _context;

    public PacienteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Paciente> ObterPorId(long id)
    {
        var paciente = await _context.Pacientes.FindAsync(id);
        if (paciente is null)
            throw new KeyNotFoundException($"Paciente {id} não encontrado.");
        return paciente;
    }

    public async Task<Paciente> ObterPorIdOuNulo(long id) =>
        await _context.Pacientes.FindAsync(id);

    public async Task<bool> ExisteCpfNoEstabelecimento(string cpf, long estabelecimentoId, long ignorarPacienteId) =>
        await _context.Pacientes
            .AsNoTracking()
            .AnyAsync(p =>
                p.Cpf == cpf &&
                p.EstabelecimentoId == estabelecimentoId &&
                p.DeletadoEm == null &&
                p.Id != ignorarPacienteId);

    public async Task Salvar(Paciente paciente)
    {
        if (paciente.Id == 0)
        {
            await _context.Pacientes.AddAsync(paciente);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.Pacientes.Update(paciente);
        }
    }
}
