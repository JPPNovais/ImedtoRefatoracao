using Imedto.Backend.Domain.Termos;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Write-side de modelos de termo. Sempre filtra por <c>estabelecimento_id</c>
/// (defense-in-depth IDOR/LGPD). Padrões do sistema (estab IS NULL) têm método
/// dedicado.
/// </summary>
public sealed class TermoModeloRepository : ITermoModeloRepository
{
    private readonly AppDbContext _context;

    public TermoModeloRepository(AppDbContext context) => _context = context;

    public Task<TermoModelo> ObterPorIdDoEstabelecimentoOuNulo(long id, long estabelecimentoId) =>
        _context.TermosModelo
            .FirstOrDefaultAsync(m =>
                m.Id == id &&
                m.EstabelecimentoId == estabelecimentoId &&
                m.DeletadoEm == null);

    public Task<TermoModelo> ObterPadraoDoSistemaPorIdOuNulo(long id) =>
        _context.TermosModelo
            .FirstOrDefaultAsync(m =>
                m.Id == id &&
                m.EstabelecimentoId == null &&
                m.DeletadoEm == null);

    public async Task Salvar(TermoModelo modelo)
    {
        if (modelo.Id == 0)
        {
            await _context.TermosModelo.AddAsync(modelo);
            // SaveChanges aqui resolve o Id — necessário para o snapshot da versão 1.
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.TermosModelo.Update(modelo);
        }
    }

    public async Task SalvarVersao(TermoModeloVersao versao)
    {
        await _context.TermosModeloVersao.AddAsync(versao);
    }
}
