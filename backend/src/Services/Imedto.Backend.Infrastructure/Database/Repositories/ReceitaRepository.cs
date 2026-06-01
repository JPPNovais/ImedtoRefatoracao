using Imedto.Backend.Domain.Receitas;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ReceitaRepository : IReceitaRepository
{
    private readonly AppDbContext _context;

    public ReceitaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Receita?> ObterPorIdOuNulo(long id, long estabelecimentoId) =>
        await _context.Receitas
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.Id == id && x.EstabelecimentoId == estabelecimentoId);

    /// <inheritdoc />
    public async Task<Receita?> ObterSemTenantAsync(long id) =>
        await _context.Receitas
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.Id == id);

    /// <inheritdoc />
    public async Task<List<Receita>> ListarPendentesParaExpirarAsync(DateTime anteriorA, CancellationToken ct = default) =>
        await _context.Receitas
            .Where(r => r.AssinaturaDigitalStatus == StatusAssinaturaDigital.AssinaturaPendente
                     && r.AssinaturaSolicitadaEm != null
                     && r.AssinaturaSolicitadaEm < anteriorA)
            .ToListAsync(ct);

    public async Task Salvar(Receita receita)
    {
        if (receita.Id == 0)
        {
            await _context.Receitas.AddAsync(receita);
            // SaveChanges aqui resolve o Id — necessário para o evento ReceitaEmitida.
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.Receitas.Update(receita);
        }
    }
}

public class ConfiguracaoReceitaRepository : IConfiguracaoReceitaRepository
{
    private readonly AppDbContext _context;

    public ConfiguracaoReceitaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ConfiguracaoReceitaEstabelecimento?> ObterPorEstabelecimentoOuNulo(long estabelecimentoId) =>
        await _context.ConfiguracoesReceita
            .FirstOrDefaultAsync(c => c.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(ConfiguracaoReceitaEstabelecimento configuracao)
    {
        // PK é manualmente definida (EstabelecimentoId). Detecta por presença no banco.
        var existente = await _context.ConfiguracoesReceita
            .AnyAsync(c => c.EstabelecimentoId == configuracao.EstabelecimentoId);

        if (!existente)
            await _context.ConfiguracoesReceita.AddAsync(configuracao);
        else
            _context.ConfiguracoesReceita.Update(configuracao);
    }
}

/// <summary>
/// Implementa o "upsert" lógico de favoritos: se já existe (profissional, estab, medicamento, posologia),
/// incrementa o uso; senão, cria com count=1. Roda dentro da transação da request via <c>AppDbContext</c>.
/// </summary>
public class MedicamentoFavoritoRepository : IMedicamentoFavoritoRepository
{
    private readonly AppDbContext _context;

    public MedicamentoFavoritoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarUso(
        Guid profissionalUsuarioId,
        long estabelecimentoId,
        string medicamento,
        string? posologia,
        ViaAdministracao? via)
    {
        var medicamentoNorm = medicamento.Trim();
        var posologiaNorm = string.IsNullOrWhiteSpace(posologia) ? null : posologia.Trim();

        var favorito = await _context.MedicamentosFavoritos
            .FirstOrDefaultAsync(f =>
                f.ProfissionalUsuarioId == profissionalUsuarioId &&
                f.EstabelecimentoId == estabelecimentoId &&
                f.Medicamento == medicamentoNorm &&
                f.Posologia == posologiaNorm);

        if (favorito is null)
        {
            favorito = MedicamentoFavorito.CriarOuIncrementar(
                profissionalUsuarioId, estabelecimentoId, medicamentoNorm, posologiaNorm, via);
            await _context.MedicamentosFavoritos.AddAsync(favorito);
        }
        else
        {
            favorito.IncrementarUso();
        }
    }
}
