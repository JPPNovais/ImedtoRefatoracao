using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Agendamentos;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ListaEsperaRepository : IListaEsperaRepository
{
    private readonly AppDbContext _db;
    public ListaEsperaRepository(AppDbContext db) => _db = db;

    public Task<ListaEsperaAgendamento?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => _db.ListaEsperaAgendamentos
            .FirstOrDefaultAsync(x => x.Id == id && x.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(ListaEsperaAgendamento entity)
    {
        if (entity.Id == 0) _db.ListaEsperaAgendamentos.Add(entity);
        else _db.ListaEsperaAgendamentos.Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task Remover(ListaEsperaAgendamento entity)
    {
        _db.ListaEsperaAgendamentos.Remove(entity);
        await _db.SaveChangesAsync();
    }
}
