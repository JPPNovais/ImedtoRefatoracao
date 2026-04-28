using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.ModelosPermissao;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ModeloPermissaoRepository : IModeloPermissaoRepository
{
    private readonly AppDbContext _context;

    public ModeloPermissaoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ModeloPermissaoEstabelecimento> ObterPorId(long id)
    {
        var modelo = await _context.ModelosPermissao.FindAsync(id);
        if (modelo is null)
            throw new KeyNotFoundException($"Modelo de permissão {id} não encontrado.");
        return modelo;
    }

    public async Task<ModeloPermissaoEstabelecimento> ObterPadraoDoEstabelecimento(long estabelecimentoId) =>
        await _context.ModelosPermissao
            .FirstOrDefaultAsync(m => m.EstabelecimentoId == estabelecimentoId && m.EhPadrao);

    public async Task<bool> PertenceAoEstabelecimento(long modeloId, long estabelecimentoId) =>
        await _context.ModelosPermissao
            .AsNoTracking()
            .AnyAsync(m => m.Id == modeloId && m.EstabelecimentoId == estabelecimentoId);

    public async Task<bool> EstaEmUsoPorVinculoAtivo(long modeloId) =>
        await _context.Vinculos
            .AsNoTracking()
            .AnyAsync(v => v.ModeloPermissaoId == modeloId
                        && v.Status != Domain.Vinculos.VinculoStatus.Inativo);

    public async Task Salvar(ModeloPermissaoEstabelecimento modelo)
    {
        if (modelo.Id == 0)
            await _context.ModelosPermissao.AddAsync(modelo);
        else
            _context.ModelosPermissao.Update(modelo);
    }

    public Task Excluir(ModeloPermissaoEstabelecimento modelo)
    {
        _context.ModelosPermissao.Remove(modelo);
        return Task.CompletedTask;
    }
}
