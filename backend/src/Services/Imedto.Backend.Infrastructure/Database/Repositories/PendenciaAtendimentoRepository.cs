using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Prontuarios.Pendencias;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class PendenciaAtendimentoRepository : IPendenciaAtendimentoRepository
{
    private readonly AppDbContext _db;

    public PendenciaAtendimentoRepository(AppDbContext db) => _db = db;

    public async Task Salvar(PendenciaAtendimento pendencia)
    {
        if (pendencia.Id == 0)
            _db.PendenciasAtendimento.Add(pendencia);
        await _db.SaveChangesAsync();
    }

    public async Task<PendenciaAtendimento?> ObterAbertaMaisRecentePorAcao(
        long estabelecimentoId,
        long pacienteId,
        AcaoPendencia acao)
        => await _db.PendenciasAtendimento
            .Where(p =>
                p.EstabelecimentoId == estabelecimentoId &&
                p.PacienteId == pacienteId &&
                p.Acao == acao &&
                p.Status == StatusPendencia.Pendente)
            .OrderByDescending(p => p.CriadoEm)
            .FirstOrDefaultAsync();

    public async Task<PendenciaAtendimento?> ObterPorId(long id, long estabelecimentoId)
        => await _db.PendenciasAtendimento
            .FirstOrDefaultAsync(p => p.Id == id && p.EstabelecimentoId == estabelecimentoId);

    public async Task<bool> ExistePorEvolucaoEAcao(long evolucaoId, AcaoPendencia acao)
        => await _db.PendenciasAtendimento
            .AnyAsync(p => p.EvolucaoId == evolucaoId && p.Acao == acao);
}
