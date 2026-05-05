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

    public Task<SolicitacaoVinculo?> ObterPorIdOuNulo(long id) =>
        _context.Set<SolicitacaoVinculo>().FirstOrDefaultAsync(s => s.Id == id);

    public Task<SolicitacaoVinculo?> ObterPorIdNoEstabelecimentoOuNulo(long id, long estabelecimentoId) =>
        _context.Set<SolicitacaoVinculo>()
            .FirstOrDefaultAsync(s => s.Id == id && s.EstabelecimentoId == estabelecimentoId);

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
