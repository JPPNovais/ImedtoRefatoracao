using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Automacoes;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class RegraAutomacaoRepository : IRegraAutomacaoRepository
{
    private readonly AppDbContext _db;

    public RegraAutomacaoRepository(AppDbContext db) => _db = db;

    public async Task<RegraAutomacao?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => await _db.RegrasAutomacao
            .FirstOrDefaultAsync(r => r.Id == id && r.EstabelecimentoId == estabelecimentoId);

#pragma warning disable CS0618 // sobrecarga sem tenant — restrito ao ProcessadorAutomacoesJob
    public async Task<RegraAutomacao?> ObterPorIdOuNuloSemTenant(long id)
        => await _db.RegrasAutomacao.FirstOrDefaultAsync(r => r.Id == id);
#pragma warning restore CS0618

    public async Task<List<RegraAutomacao>> ListarAtivasPorEvento(long estabelecimentoId, string evento)
        => await _db.RegrasAutomacao
            .AsNoTracking()
            .Where(r => r.EstabelecimentoId == estabelecimentoId
                     && r.EventoGatilho == evento
                     && r.Ativa)
            .ToListAsync();

    public async Task<List<RegraAutomacao>> ListarPorEstabelecimento(long estabelecimentoId)
        => await _db.RegrasAutomacao
            .AsNoTracking()
            .Where(r => r.EstabelecimentoId == estabelecimentoId)
            .OrderByDescending(r => r.CriadoEm)
            .ToListAsync();

    public async Task Salvar(RegraAutomacao regra)
    {
        if (regra.Id == 0)
            _db.RegrasAutomacao.Add(regra);
        else
            _db.RegrasAutomacao.Update(regra);
        await _db.SaveChangesAsync();
    }
}
