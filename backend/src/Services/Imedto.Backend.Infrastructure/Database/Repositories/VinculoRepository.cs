using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Vinculos;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class VinculoRepository : IVinculoRepository
{
    private readonly AppDbContext _context;

    public VinculoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<VinculoProfissionalEstabelecimento> ObterPorId(long id)
    {
        var v = await _context.Vinculos.FindAsync(id);
        if (v is null)
            throw new KeyNotFoundException($"Vínculo {id} não encontrado.");
        return v;
    }

    public async Task<VinculoProfissionalEstabelecimento?> ObterPorIdOuNulo(long id) =>
        await _context.Vinculos.FindAsync(id);

    public async Task<VinculoProfissionalEstabelecimento> ObterVinculoAtivoOuPendente(Guid profissionalUsuarioId, long estabelecimentoId) =>
        await _context.Vinculos
            .FirstOrDefaultAsync(v =>
                v.ProfissionalUsuarioId == profissionalUsuarioId &&
                v.EstabelecimentoId == estabelecimentoId &&
                v.Status != VinculoStatus.Inativo);

    public async Task<VinculoProfissionalEstabelecimento?> ObterPorProfissionalEEstabelecimentoOuNulo(
        Guid profissionalUsuarioId, long estabelecimentoId) =>
        await _context.Vinculos
            .FirstOrDefaultAsync(v =>
                v.ProfissionalUsuarioId == profissionalUsuarioId &&
                v.EstabelecimentoId == estabelecimentoId);

    public async Task<bool> PodeAtuarComoProfissional(Guid usuarioId, long estabelecimentoId)
    {
        // Regra unificada: vínculo não-inativo OU é o dono. Manter sincronizada com o
        // SQL de VinculoQueryRepository.ListarProfissionaisDoEstabelecimento.
        var temVinculo = await _context.Vinculos.AnyAsync(v =>
            v.ProfissionalUsuarioId == usuarioId &&
            v.EstabelecimentoId == estabelecimentoId &&
            v.Status != VinculoStatus.Inativo);
        if (temVinculo) return true;

        return await _context.Estabelecimentos.AnyAsync(e =>
            e.Id == estabelecimentoId && e.DonoUsuarioId == usuarioId);
    }

    public async Task Salvar(VinculoProfissionalEstabelecimento vinculo)
    {
        if (vinculo.Id == 0)
        {
            await _context.Vinculos.AddAsync(vinculo);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.Vinculos.Update(vinculo);
        }
    }
}
