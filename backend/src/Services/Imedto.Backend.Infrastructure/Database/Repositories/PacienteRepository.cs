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

    public async Task<Paciente?> ObterPorIdOuNulo(long id, long estabelecimentoId) =>
        await _context.Pacientes
            .FirstOrDefaultAsync(p => p.Id == id && p.EstabelecimentoId == estabelecimentoId);

#pragma warning disable CS0618 // sobrecarga sem tenant — restrito ao AnonimizacaoService
    public async Task<Paciente?> ObterPorIdOuNulo(long id) =>
        await _context.Pacientes.FindAsync(id);
#pragma warning restore CS0618

    public async Task<bool> ExisteCpfNoEstabelecimento(string cpf, long estabelecimentoId, long ignorarPacienteId) =>
        await _context.Pacientes
            .AsNoTracking()
            .AnyAsync(p =>
                p.Cpf == cpf &&
                p.EstabelecimentoId == estabelecimentoId &&
                p.DeletadoEm == null &&
                p.Id != ignorarPacienteId);

    public async Task<bool> ExisteDocumentoInternacionalNoEstabelecimento(string documento, long estabelecimentoId, long ignorarPacienteId) =>
        await _context.Pacientes
            .AsNoTracking()
            .AnyAsync(p =>
                p.DocumentoInternacional == documento &&
                p.EstabelecimentoId == estabelecimentoId &&
                p.DeletadoEm == null &&
                p.Id != ignorarPacienteId);

    public async Task Salvar(Paciente paciente)
    {
        if (paciente.Id == 0)
            await _context.Pacientes.AddAsync(paciente);
        else
            _context.Pacientes.Update(paciente);
        await _context.SaveChangesAsync();
    }
}
