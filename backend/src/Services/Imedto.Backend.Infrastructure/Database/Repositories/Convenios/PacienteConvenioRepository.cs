using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.PacienteConvenios;

namespace Imedto.Backend.Infrastructure.Database.Repositories.Convenios;

public class PacienteConvenioRepository : IPacienteConvenioRepository
{
    private readonly AppDbContext _db;
    public PacienteConvenioRepository(AppDbContext db) => _db = db;

    public async Task<PacienteConvenio?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => await _db.PacienteConvenios
            .FirstOrDefaultAsync(pc => pc.Id == id && pc.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(PacienteConvenio pacienteConvenio)
    {
        if (pacienteConvenio.Id == 0)
            _db.PacienteConvenios.Add(pacienteConvenio);
        else
            _db.PacienteConvenios.Update(pacienteConvenio);
        await _db.SaveChangesAsync();
    }

    public async Task Excluir(PacienteConvenio pacienteConvenio)
    {
        _db.PacienteConvenios.Remove(pacienteConvenio);
        await _db.SaveChangesAsync();
    }
}
