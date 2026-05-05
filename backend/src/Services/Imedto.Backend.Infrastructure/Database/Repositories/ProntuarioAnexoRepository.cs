using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ProntuarioAnexoRepository : IProntuarioAnexoRepository
{
    private readonly AppDbContext _context;

    public ProntuarioAnexoRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<ProntuarioAnexo?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => _context.ProntuarioAnexos
            .FirstOrDefaultAsync(a => a.Id == id && a.EstabelecimentoId == estabelecimentoId);

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
