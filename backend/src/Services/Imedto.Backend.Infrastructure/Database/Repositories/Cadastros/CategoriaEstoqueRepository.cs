using Imedto.Backend.Domain.Inventario.Cadastros;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Infrastructure.Database.Repositories.Cadastros;

public class CategoriaEstoqueRepository : ICategoriaEstoqueRepository
{
    private readonly AppDbContext _db;
    public CategoriaEstoqueRepository(AppDbContext db) => _db = db;

    public async Task<CategoriaEstoque?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => await _db.Set<CategoriaEstoque>()
            .FirstOrDefaultAsync(c => c.Id == id && c.EstabelecimentoId == estabelecimentoId);

    public async Task<bool> ExisteComNomeNoEstabelecimento(string nome, long estabelecimentoId, long? ignorarId = null)
    {
        if (string.IsNullOrWhiteSpace(nome)) return false;
        var norm = nome.Trim().ToLower();
        return await _db.Set<CategoriaEstoque>()
            .AsNoTracking()
            .AnyAsync(c => c.EstabelecimentoId == estabelecimentoId
                           && c.Nome.ToLower() == norm
                           && (!ignorarId.HasValue || c.Id != ignorarId.Value));
    }

    public async Task<bool> ExistemItensVinculados(long categoriaId, long estabelecimentoId)
        => await _db.ItensInventario.AsNoTracking()
            .AnyAsync(i => i.CategoriaId == categoriaId && i.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(CategoriaEstoque categoria)
    {
        if (categoria.Id == 0)
            _db.Set<CategoriaEstoque>().Add(categoria);
        else
            _db.Set<CategoriaEstoque>().Update(categoria);
        await _db.SaveChangesAsync();
    }
}

public class FabricanteEstoqueRepository : IFabricanteEstoqueRepository
{
    private readonly AppDbContext _db;
    public FabricanteEstoqueRepository(AppDbContext db) => _db = db;

    public async Task<FabricanteEstoque?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => await _db.Set<FabricanteEstoque>()
            .FirstOrDefaultAsync(f => f.Id == id && f.EstabelecimentoId == estabelecimentoId);

    public async Task<bool> ExisteComNomeNoEstabelecimento(string nome, long estabelecimentoId, long? ignorarId = null)
    {
        if (string.IsNullOrWhiteSpace(nome)) return false;
        var norm = nome.Trim().ToLower();
        return await _db.Set<FabricanteEstoque>()
            .AsNoTracking()
            .AnyAsync(f => f.EstabelecimentoId == estabelecimentoId
                           && f.Nome.ToLower() == norm
                           && (!ignorarId.HasValue || f.Id != ignorarId.Value));
    }

    public async Task<bool> ExistemItensVinculados(long fabricanteId, long estabelecimentoId)
        => await _db.ItensInventario.AsNoTracking()
            .AnyAsync(i => i.FabricanteId == fabricanteId && i.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(FabricanteEstoque fabricante)
    {
        if (fabricante.Id == 0)
            _db.Set<FabricanteEstoque>().Add(fabricante);
        else
            _db.Set<FabricanteEstoque>().Update(fabricante);
        await _db.SaveChangesAsync();
    }
}

public class FornecedorEstoqueRepository : IFornecedorEstoqueRepository
{
    private readonly AppDbContext _db;
    public FornecedorEstoqueRepository(AppDbContext db) => _db = db;

    public async Task<FornecedorEstoque?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => await _db.Set<FornecedorEstoque>()
            .FirstOrDefaultAsync(f => f.Id == id && f.EstabelecimentoId == estabelecimentoId);

    public async Task<bool> ExisteComNomeNoEstabelecimento(string razaoSocial, long estabelecimentoId, long? ignorarId = null)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial)) return false;
        var norm = razaoSocial.Trim().ToLower();
        return await _db.Set<FornecedorEstoque>()
            .AsNoTracking()
            .AnyAsync(f => f.EstabelecimentoId == estabelecimentoId
                           && f.RazaoSocial.ToLower() == norm
                           && (!ignorarId.HasValue || f.Id != ignorarId.Value));
    }

    public async Task<bool> ExisteComCnpjNoEstabelecimento(string cnpj, long estabelecimentoId, long? ignorarId = null)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return false;
        return await _db.Set<FornecedorEstoque>()
            .AsNoTracking()
            .AnyAsync(f => f.EstabelecimentoId == estabelecimentoId
                           && f.Cnpj == cnpj
                           && (!ignorarId.HasValue || f.Id != ignorarId.Value));
    }

    public async Task<bool> ExistemItensVinculados(long fornecedorId, long estabelecimentoId)
        => await _db.ItensInventario.AsNoTracking()
            .AnyAsync(i => i.FornecedorPadraoId == fornecedorId && i.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(FornecedorEstoque fornecedor)
    {
        if (fornecedor.Id == 0)
            _db.Set<FornecedorEstoque>().Add(fornecedor);
        else
            _db.Set<FornecedorEstoque>().Update(fornecedor);
        await _db.SaveChangesAsync();
    }
}

public class LocalEstoqueRepository : ILocalEstoqueRepository
{
    private readonly AppDbContext _db;
    public LocalEstoqueRepository(AppDbContext db) => _db = db;

    public async Task<LocalEstoque?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => await _db.Set<LocalEstoque>()
            .FirstOrDefaultAsync(l => l.Id == id && l.EstabelecimentoId == estabelecimentoId);

    public async Task<bool> ExisteComNomeNoEstabelecimento(string nome, long estabelecimentoId, long? ignorarId = null)
    {
        if (string.IsNullOrWhiteSpace(nome)) return false;
        var norm = nome.Trim().ToLower();
        return await _db.Set<LocalEstoque>()
            .AsNoTracking()
            .AnyAsync(l => l.EstabelecimentoId == estabelecimentoId
                           && l.Nome.ToLower() == norm
                           && (!ignorarId.HasValue || l.Id != ignorarId.Value));
    }

    public async Task<bool> ExistemItensVinculados(long localId, long estabelecimentoId)
        => await _db.ItensInventario.AsNoTracking()
            .AnyAsync(i => i.LocalPadraoId == localId && i.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(LocalEstoque local)
    {
        if (local.Id == 0)
            _db.Set<LocalEstoque>().Add(local);
        else
            _db.Set<LocalEstoque>().Update(local);
        await _db.SaveChangesAsync();
    }
}
