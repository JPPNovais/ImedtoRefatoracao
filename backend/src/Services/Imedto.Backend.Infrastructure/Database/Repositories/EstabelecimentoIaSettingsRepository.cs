using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Ia;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class EstabelecimentoIaSettingsRepository : IEstabelecimentoIaSettingsRepository
{
    private readonly AppDbContext _context;

    public EstabelecimentoIaSettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EstabelecimentoIaSettings?> ObterPorEstabelecimentoOuNulo(
        long estabelecimentoId,
        CancellationToken ct = default)
    {
        // AsNoTracking porque o decorator só lê — UPDATE acontece via Salvar() abaixo.
        return await _context.EstabelecimentosIaSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == estabelecimentoId, ct);
    }

    public async Task Salvar(EstabelecimentoIaSettings settings, CancellationToken ct = default)
    {
        var existente = await _context.EstabelecimentosIaSettings
            .FirstOrDefaultAsync(s => s.Id == settings.Id, ct);

        if (existente is null)
        {
            await _context.EstabelecimentosIaSettings.AddAsync(settings, ct);
        }
        else
        {
            // Aggregate veio do controller (criado via fábrica) — copiamos os campos
            // mutáveis para a instância tracked.
            _context.Entry(existente).CurrentValues.SetValues(settings);
        }
    }
}
