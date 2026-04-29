using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Vinculos;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class SolicitacaoVinculoRepository : ISolicitacaoVinculoRepository
{
    private readonly AppDbContext _context;

    public SolicitacaoVinculoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SolicitacaoVinculo> ObterPorId(long id)
    {
        var s = await _context.Set<SolicitacaoVinculo>().FindAsync(id);
        if (s is null)
            throw new KeyNotFoundException($"Solicitação de vínculo {id} não encontrada.");
        return s;
    }

    public Task<SolicitacaoVinculo> ObterPorIdOuNulo(long id) =>
        _context.Set<SolicitacaoVinculo>().FirstOrDefaultAsync(s => s.Id == id);

    public Task<SolicitacaoVinculo> ObterPendentePorProfissionalEEstab(
        Guid profissionalUsuarioId, long estabelecimentoId) =>
        _context.Set<SolicitacaoVinculo>()
            .FirstOrDefaultAsync(s =>
                s.ProfissionalUsuarioId == profissionalUsuarioId &&
                s.EstabelecimentoId == estabelecimentoId &&
                s.Status == StatusSolicitacaoVinculo.Pendente);

    public async Task Salvar(SolicitacaoVinculo solicitacao)
    {
        if (solicitacao.Id == 0)
        {
            await _context.Set<SolicitacaoVinculo>().AddAsync(solicitacao);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.Set<SolicitacaoVinculo>().Update(solicitacao);
        }
    }
}
