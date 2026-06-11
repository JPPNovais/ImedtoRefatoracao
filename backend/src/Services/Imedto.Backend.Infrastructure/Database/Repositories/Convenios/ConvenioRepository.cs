using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Convenios;

namespace Imedto.Backend.Infrastructure.Database.Repositories.Convenios;

public class ConvenioRepository : IConvenioRepository
{
    private readonly AppDbContext _db;
    public ConvenioRepository(AppDbContext db) => _db = db;

    public async Task<Convenio?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => await _db.Convenios
            .Include(c => c.Planos)
            .FirstOrDefaultAsync(c => c.Id == id && c.EstabelecimentoId == estabelecimentoId);

    public async Task<bool> TemCarteirinhasOuCobrancas(long convenioId)
    {
        // R3: verifica se há carteirinhas ou cobranças referenciando o convênio
        var temCarteirinha = await _db.PacienteConvenios
            .AnyAsync(pc => pc.ConvenioId == convenioId);
        if (temCarteirinha) return true;

        var temCobranca = await _db.Cobrancas
            .AnyAsync(c => c.ConvenioId == convenioId);
        return temCobranca;
    }

    public async Task Salvar(Convenio convenio)
    {
        if (convenio.Id == 0)
            _db.Convenios.Add(convenio);
        else
            _db.Convenios.Update(convenio);
        await _db.SaveChangesAsync();
    }

    public async Task Excluir(Convenio convenio)
    {
        _db.Convenios.Remove(convenio);
        await _db.SaveChangesAsync();
    }
}
