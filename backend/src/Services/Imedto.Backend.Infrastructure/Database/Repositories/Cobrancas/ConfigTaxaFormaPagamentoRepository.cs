using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Cobrancas;

namespace Imedto.Backend.Infrastructure.Database.Repositories.Cobrancas;

public class ConfigTaxaFormaPagamentoRepository : IConfigTaxaFormaPagamentoRepository
{
    private readonly AppDbContext _db;

    public ConfigTaxaFormaPagamentoRepository(AppDbContext db) => _db = db;

    public async Task<ConfigTaxaFormaPagamento?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => await _db.ConfigTaxasFormaPagamento
            .FirstOrDefaultAsync(c => c.Id == id && c.EstabelecimentoId == estabelecimentoId);

    public async Task<ConfigTaxaFormaPagamento?> ObterPorForma(long estabelecimentoId, long formaPagamentoId)
        => await _db.ConfigTaxasFormaPagamento
            .FirstOrDefaultAsync(c => c.EstabelecimentoId == estabelecimentoId
                                      && c.FormaPagamentoId == formaPagamentoId
                                      && c.Ativo);

    public async Task Salvar(ConfigTaxaFormaPagamento config)
    {
        if (config.Id == 0)
            _db.ConfigTaxasFormaPagamento.Add(config);
        else
            _db.ConfigTaxasFormaPagamento.Update(config);
        await _db.SaveChangesAsync();
    }
}
