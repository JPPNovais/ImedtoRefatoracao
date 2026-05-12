using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Financeiro;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class FormaPagamentoRepository : IFormaPagamentoRepository
{
    private readonly AppDbContext _db;

    public FormaPagamentoRepository(AppDbContext db) => _db = db;

    public async Task<FormaPagamento?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => await _db.FormasPagamento
            .FirstOrDefaultAsync(f => f.Id == id && f.EstabelecimentoId == estabelecimentoId);

    public async Task<bool> ExisteComNomeNoEstabelecimento(string nome, long estabelecimentoId)
    {
        if (string.IsNullOrWhiteSpace(nome)) return false;
        var nomeNorm = nome.Trim();
        return await _db.FormasPagamento
            .AsNoTracking()
            .AnyAsync(f => f.EstabelecimentoId == estabelecimentoId && f.Nome == nomeNorm);
    }

    public async Task Salvar(FormaPagamento forma)
    {
        if (forma.Id == 0)
            _db.FormasPagamento.Add(forma);
        else
            _db.FormasPagamento.Update(forma);
        await _db.SaveChangesAsync();
    }
}
