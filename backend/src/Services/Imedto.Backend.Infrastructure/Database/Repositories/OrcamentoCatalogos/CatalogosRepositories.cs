using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.Orcamentos.Catalogos;

namespace Imedto.Backend.Infrastructure.Database.Repositories.OrcamentoCatalogos;

public class CatalogoCirurgiaRepository : ICatalogoCirurgiaRepository
{
    private readonly AppDbContext _db;
    public CatalogoCirurgiaRepository(AppDbContext db) => _db = db;

    public Task<CatalogoCirurgia?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => _db.CatalogoCirurgias.FirstOrDefaultAsync(x => x.Id == id && x.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(CatalogoCirurgia entity)
    {
        if (entity.Id == 0) _db.CatalogoCirurgias.Add(entity);
        else _db.CatalogoCirurgias.Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task Remover(CatalogoCirurgia entity)
    {
        _db.CatalogoCirurgias.Remove(entity);
        await _db.SaveChangesAsync();
    }
}

public class ValorProfissionalOrcamentoRepository : IValorProfissionalOrcamentoRepository
{
    private readonly AppDbContext _db;
    public ValorProfissionalOrcamentoRepository(AppDbContext db) => _db = db;

    public Task<ValorProfissionalOrcamento?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => _db.ValoresProfissionalOrcamento.FirstOrDefaultAsync(x => x.Id == id && x.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(ValorProfissionalOrcamento entity)
    {
        if (entity.Id == 0) _db.ValoresProfissionalOrcamento.Add(entity);
        else _db.ValoresProfissionalOrcamento.Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task Remover(ValorProfissionalOrcamento entity)
    {
        _db.ValoresProfissionalOrcamento.Remove(entity);
        await _db.SaveChangesAsync();
    }
}

public class ConfiguracaoLocalCirurgiaRepository : IConfiguracaoLocalCirurgiaRepository
{
    private readonly AppDbContext _db;
    public ConfiguracaoLocalCirurgiaRepository(AppDbContext db) => _db = db;

    public Task<ConfiguracaoLocalCirurgia?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => _db.ConfiguracoesLocalCirurgia.FirstOrDefaultAsync(x => x.Id == id && x.EstabelecimentoId == estabelecimentoId);

    public Task<ConfiguracaoLocalCirurgia?> ObterPorEstabelecimentoETipo(long estabelecimentoId, TipoLocalCirurgia tipo)
        => _db.ConfiguracoesLocalCirurgia.FirstOrDefaultAsync(x => x.EstabelecimentoId == estabelecimentoId && x.TipoLocal == tipo);

    public async Task<IReadOnlyList<ConfiguracaoLocalCirurgia>> ListarDoEstabelecimento(long estabelecimentoId)
        => await _db.ConfiguracoesLocalCirurgia
            .Where(x => x.EstabelecimentoId == estabelecimentoId)
            .OrderBy(x => x.TipoLocal)
            .ToListAsync();

    public async Task Salvar(ConfiguracaoLocalCirurgia entity)
    {
        if (entity.Id == 0) _db.ConfiguracoesLocalCirurgia.Add(entity);
        else _db.ConfiguracoesLocalCirurgia.Update(entity);
        await _db.SaveChangesAsync();
    }
}

public class CatalogoEquipeEspecializadaRepository : ICatalogoEquipeEspecializadaRepository
{
    private readonly AppDbContext _db;
    public CatalogoEquipeEspecializadaRepository(AppDbContext db) => _db = db;

    public Task<CatalogoEquipeEspecializada?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => _db.CatalogoEquipesEspecializadas.FirstOrDefaultAsync(x => x.Id == id && x.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(CatalogoEquipeEspecializada entity)
    {
        if (entity.Id == 0) _db.CatalogoEquipesEspecializadas.Add(entity);
        else _db.CatalogoEquipesEspecializadas.Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task Remover(CatalogoEquipeEspecializada entity)
    {
        _db.CatalogoEquipesEspecializadas.Remove(entity);
        await _db.SaveChangesAsync();
    }
}

public class CatalogoImplanteRepository : ICatalogoImplanteRepository
{
    private readonly AppDbContext _db;
    public CatalogoImplanteRepository(AppDbContext db) => _db = db;

    public Task<CatalogoImplante?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => _db.CatalogoImplantes.FirstOrDefaultAsync(x => x.Id == id && x.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(CatalogoImplante entity)
    {
        if (entity.Id == 0) _db.CatalogoImplantes.Add(entity);
        else _db.CatalogoImplantes.Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task Remover(CatalogoImplante entity)
    {
        _db.CatalogoImplantes.Remove(entity);
        await _db.SaveChangesAsync();
    }
}

public class CatalogoProdutoRepository : ICatalogoProdutoRepository
{
    private readonly AppDbContext _db;
    public CatalogoProdutoRepository(AppDbContext db) => _db = db;

    public Task<CatalogoProduto?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => _db.CatalogoProdutos.FirstOrDefaultAsync(x => x.Id == id && x.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(CatalogoProduto entity)
    {
        if (entity.Id == 0) _db.CatalogoProdutos.Add(entity);
        else _db.CatalogoProdutos.Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task Remover(CatalogoProduto entity)
    {
        _db.CatalogoProdutos.Remove(entity);
        await _db.SaveChangesAsync();
    }
}

public class CatalogoCirurgiaProdutoRepository : ICatalogoCirurgiaProdutoRepository
{
    private readonly AppDbContext _db;
    public CatalogoCirurgiaProdutoRepository(AppDbContext db) => _db = db;

    public Task<CatalogoCirurgiaProduto?> ObterPorIdOuNulo(long id)
        => _db.CatalogoCirurgiaProdutos.FirstOrDefaultAsync(x => x.Id == id);

    public Task<CatalogoCirurgiaProduto?> ObterPorCirurgiaProduto(long catalogoCirurgiaId, long catalogoProdutoId)
        => _db.CatalogoCirurgiaProdutos.FirstOrDefaultAsync(
            x => x.CatalogoCirurgiaId == catalogoCirurgiaId && x.CatalogoProdutoId == catalogoProdutoId);

    public async Task<IReadOnlyList<CatalogoCirurgiaProduto>> ListarDaCirurgia(long catalogoCirurgiaId)
        => await _db.CatalogoCirurgiaProdutos.Where(x => x.CatalogoCirurgiaId == catalogoCirurgiaId).ToListAsync();

    public async Task Salvar(CatalogoCirurgiaProduto entity)
    {
        if (entity.Id == 0) _db.CatalogoCirurgiaProdutos.Add(entity);
        else _db.CatalogoCirurgiaProdutos.Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task Remover(CatalogoCirurgiaProduto entity)
    {
        _db.CatalogoCirurgiaProdutos.Remove(entity);
        await _db.SaveChangesAsync();
    }
}

public class ConfiguracaoPagamentoCatalogoRepository : IConfiguracaoPagamentoCatalogoRepository
{
    private readonly AppDbContext _db;
    public ConfiguracaoPagamentoCatalogoRepository(AppDbContext db) => _db = db;

    public Task<ConfiguracaoPagamentoCatalogo?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => _db.ConfiguracoesPagamentoCatalogo.FirstOrDefaultAsync(x => x.Id == id && x.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(ConfiguracaoPagamentoCatalogo entity)
    {
        if (entity.Id == 0) _db.ConfiguracoesPagamentoCatalogo.Add(entity);
        else _db.ConfiguracoesPagamentoCatalogo.Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task Remover(ConfiguracaoPagamentoCatalogo entity)
    {
        _db.ConfiguracoesPagamentoCatalogo.Remove(entity);
        await _db.SaveChangesAsync();
    }
}

public class OrcamentoTeamRoleRepository : IOrcamentoTeamRoleRepository
{
    private readonly AppDbContext _db;
    public OrcamentoTeamRoleRepository(AppDbContext db) => _db = db;

    public Task<OrcamentoTeamRole?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => _db.OrcamentoTeamRoles.FirstOrDefaultAsync(x => x.Id == id && x.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(OrcamentoTeamRole entity)
    {
        if (entity.Id == 0) _db.OrcamentoTeamRoles.Add(entity);
        else _db.OrcamentoTeamRoles.Update(entity);
        await _db.SaveChangesAsync();
    }
}

public class OrcamentoAnestesistaRepository : IOrcamentoAnestesistaRepository
{
    private readonly AppDbContext _db;
    public OrcamentoAnestesistaRepository(AppDbContext db) => _db = db;

    public Task<OrcamentoAnestesista?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => _db.OrcamentoAnestesistas.FirstOrDefaultAsync(x => x.Id == id && x.EstabelecimentoId == estabelecimentoId);

    public Task<OrcamentoAnestesista?> ObterComFaixasOuNulo(long id, long estabelecimentoId)
        => _db.OrcamentoAnestesistas.Include("_faixas")
            .FirstOrDefaultAsync(x => x.Id == id && x.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(OrcamentoAnestesista entity)
    {
        if (entity.Id == 0) _db.OrcamentoAnestesistas.Add(entity);
        else _db.OrcamentoAnestesistas.Update(entity);
        await _db.SaveChangesAsync();
    }
}

public class OrcamentoPacoteRepository : IOrcamentoPacoteRepository
{
    private readonly AppDbContext _db;
    public OrcamentoPacoteRepository(AppDbContext db) => _db = db;

    public Task<OrcamentoPacote?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => _db.OrcamentoPacotes.FirstOrDefaultAsync(x => x.Id == id && x.EstabelecimentoId == estabelecimentoId);

    public Task<OrcamentoPacote?> ObterComAssociacoesOuNulo(long id, long estabelecimentoId)
        => _db.OrcamentoPacotes
            .Include("_procedimentos")
            .Include("_produtos")
            .Include("_teamRoles")
            .FirstOrDefaultAsync(x => x.Id == id && x.EstabelecimentoId == estabelecimentoId);

    public async Task<bool> ExistePacoteAtivoComProcedimento(long catalogoCirurgiaId, long estabelecimentoId)
    {
        return await _db.OrcamentoPacotes
            .Where(p => p.EstabelecimentoId == estabelecimentoId && p.Ativo)
            .AnyAsync(p => _db.Set<OrcamentoPacoteProcedimento>()
                .Any(pp => pp.PacoteId == p.Id && pp.CatalogoCirurgiaId == catalogoCirurgiaId));
    }

    public async Task Salvar(OrcamentoPacote entity)
    {
        if (entity.Id == 0) _db.OrcamentoPacotes.Add(entity);
        else _db.OrcamentoPacotes.Update(entity);
        await _db.SaveChangesAsync();
    }
}
