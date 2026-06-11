using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Cobrancas;

namespace Imedto.Backend.Infrastructure.Database.Repositories.Cobrancas;

public class CobrancaRepository : ICobrancaRepository
{
    private readonly AppDbContext _db;

    public CobrancaRepository(AppDbContext db) => _db = db;

    public async Task<Cobranca?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => await _db.Cobrancas
            .Include(c => c.Pagamentos)
            .Include(c => c.Estornos)
            .FirstOrDefaultAsync(c => c.Id == id && c.EstabelecimentoId == estabelecimentoId);

    public async Task<Cobranca?> ObterPorAgendamentoOuNulo(long agendamentoId, long estabelecimentoId)
        => await _db.Cobrancas
            .Include(c => c.Pagamentos)
            .Include(c => c.Estornos)
            .FirstOrDefaultAsync(c => c.AgendamentoId == agendamentoId && c.EstabelecimentoId == estabelecimentoId);

    public async Task<Cobranca?> ObterPorOrcamentoOuNulo(long orcamentoId, long estabelecimentoId)
        => await _db.Cobrancas
            .Include(c => c.Pagamentos)
            .Include(c => c.Estornos)
            .Include(c => c.HistoricoValor)
            .FirstOrDefaultAsync(c => c.OrcamentoId == orcamentoId
                                    && c.EstabelecimentoId == estabelecimentoId
                                    && c.Status != Domain.Cobrancas.StatusCobranca.Cancelada);

    public async Task Salvar(Cobranca cobranca)
    {
        if (cobranca.Id == 0)
            _db.Cobrancas.Add(cobranca);
        else
            _db.Cobrancas.Update(cobranca);
        await _db.SaveChangesAsync();
    }
}
