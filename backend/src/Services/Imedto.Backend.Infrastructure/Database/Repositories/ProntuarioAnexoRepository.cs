using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ProntuarioAnexoRepository : IProntuarioAnexoRepository
{
    private readonly AppDbContext _context;

    public ProntuarioAnexoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProntuarioAnexo> ObterPorId(long id)
    {
        var a = await _context.ProntuarioAnexos.FindAsync(id);
        if (a is null) throw new KeyNotFoundException($"Anexo {id} não encontrado.");
        return a;
    }

    public async Task Salvar(ProntuarioAnexo anexo)
    {
        if (anexo.Id == 0)
        {
            await _context.ProntuarioAnexos.AddAsync(anexo);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.ProntuarioAnexos.Update(anexo);
        }
    }
}
