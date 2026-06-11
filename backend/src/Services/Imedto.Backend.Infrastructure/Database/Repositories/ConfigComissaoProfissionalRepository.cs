using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Financeiro;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ConfigComissaoProfissionalRepository : IConfigComissaoProfissionalRepository
{
    private readonly AppDbContext _db;

    public ConfigComissaoProfissionalRepository(AppDbContext db) => _db = db;

    public async Task<ConfigComissaoProfissional?> ObterOuNulo(
        long estabelecimentoId, Guid profissionalUsuarioId, TipoComissao tipo) =>
        await _db.ConfigsComissaoProfissional
            .FirstOrDefaultAsync(c =>
                c.EstabelecimentoId == estabelecimentoId &&
                c.ProfissionalUsuarioId == profissionalUsuarioId &&
                c.Tipo == tipo);

    public async Task Salvar(ConfigComissaoProfissional config)
    {
        if (config.Id == 0)
            _db.ConfigsComissaoProfissional.Add(config);
        else
            _db.ConfigsComissaoProfissional.Update(config);
        await _db.SaveChangesAsync();
    }
}
