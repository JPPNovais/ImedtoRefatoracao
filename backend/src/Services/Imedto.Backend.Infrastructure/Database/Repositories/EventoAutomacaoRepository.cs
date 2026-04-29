using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Automacoes;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class EventoAutomacaoRepository : IEventoAutomacaoRepository
{
    private readonly AppDbContext _db;

    public EventoAutomacaoRepository(AppDbContext db) => _db = db;

    public async Task<EventoAutomacao?> ObterPorIdOuNulo(long id)
        => await _db.EventosAutomacao.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<List<EventoAutomacao>> ListarPendentesProntos(DateTime agora)
        => await _db.EventosAutomacao
            .Where(e => e.Status == StatusEventoAutomacao.Pendente && e.ExecutarEm <= agora)
            .OrderBy(e => e.ExecutarEm)
            .ToListAsync();

    public async Task<List<EventoAutomacao>> ListarParaDebug(long estabelecimentoId, string? status, int pagina, int tamanho)
    {
        if (pagina < 1) pagina = 1;
        if (tamanho < 1 || tamanho > 200) tamanho = 50;

        // Join com automation_rules para filtrar por estabelecimento (eventos não têm essa coluna).
        var query =
            from e in _db.EventosAutomacao.AsNoTracking()
            join r in _db.RegrasAutomacao.AsNoTracking() on e.RegraId equals r.Id
            where r.EstabelecimentoId == estabelecimentoId
            select e;

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StatusEventoAutomacao>(status, true, out var s))
            query = query.Where(e => e.Status == s);

        return await query
            .OrderByDescending(e => e.CriadoEm)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync();
    }

    public async Task Salvar(EventoAutomacao evento)
    {
        if (evento.Id == 0)
            _db.EventosAutomacao.Add(evento);
        else
            _db.EventosAutomacao.Update(evento);
        await _db.SaveChangesAsync();
    }
}
