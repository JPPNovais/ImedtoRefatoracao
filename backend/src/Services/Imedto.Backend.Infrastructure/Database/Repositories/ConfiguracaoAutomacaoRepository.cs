using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ConfiguracaoAutomacaoRepository : IConfiguracaoAutomacaoRepository
{
    private readonly AppDbContext _db;

    public ConfiguracaoAutomacaoRepository(AppDbContext db) => _db = db;

    public Task<ConfiguracaoAutomacao?> ObterPorEstabelecimento(long estabelecimentoId)
        => _db.ConfiguracoesAutomacao
            .FirstOrDefaultAsync(c => c.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(ConfiguracaoAutomacao config)
    {
        if (config.Id == 0)
            _db.ConfiguracoesAutomacao.Add(config);
        else
            _db.ConfiguracoesAutomacao.Update(config);
    }
}
