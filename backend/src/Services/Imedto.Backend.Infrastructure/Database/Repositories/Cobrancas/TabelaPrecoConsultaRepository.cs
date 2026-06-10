using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Cobrancas;

namespace Imedto.Backend.Infrastructure.Database.Repositories.Cobrancas;

public class TabelaPrecoConsultaRepository : ITabelaPrecoConsultaRepository
{
    private readonly AppDbContext _db;

    public TabelaPrecoConsultaRepository(AppDbContext db) => _db = db;

    public async Task<TabelaPrecoConsulta?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => await _db.TabelasPrecoConsulta
            .FirstOrDefaultAsync(t => t.Id == id && t.EstabelecimentoId == estabelecimentoId);

    /// <summary>
    /// Retorna o valor sugerido: profissional específico > padrão do estabelecimento.
    /// Retorna null se nenhum preço ativo cadastrado.
    /// </summary>
    public async Task<decimal?> ObterValorSugerido(long estabelecimentoId, Guid profissionalId)
    {
        // Tenta profissional específico primeiro
        var porfissional = await _db.TabelasPrecoConsulta
            .Where(t => t.EstabelecimentoId == estabelecimentoId
                        && t.ProfissionalId == profissionalId
                        && t.Ativo)
            .Select(t => (decimal?)t.ValorSugerido)
            .FirstOrDefaultAsync();

        if (porfissional.HasValue) return porfissional;

        // Fallback: padrão do estabelecimento (profissional_id IS NULL)
        return await _db.TabelasPrecoConsulta
            .Where(t => t.EstabelecimentoId == estabelecimentoId
                        && t.ProfissionalId == null
                        && t.Ativo)
            .Select(t => (decimal?)t.ValorSugerido)
            .FirstOrDefaultAsync();
    }

    public async Task Salvar(TabelaPrecoConsulta tabela)
    {
        if (tabela.Id == 0)
            _db.TabelasPrecoConsulta.Add(tabela);
        else
            _db.TabelasPrecoConsulta.Update(tabela);
        await _db.SaveChangesAsync();
    }
}
