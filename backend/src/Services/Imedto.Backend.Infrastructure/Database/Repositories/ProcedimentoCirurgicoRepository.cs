using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Cirurgias;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ProcedimentoCirurgicoRepository : IProcedimentoCirurgicoRepository
{
    private readonly AppDbContext _db;

    public ProcedimentoCirurgicoRepository(AppDbContext db) => _db = db;

    public async Task<ProcedimentoCirurgico?> ObterPorIdOuNulo(long id, long estabelecimentoId) =>
        await _db.ProcedimentosCirurgicos
            .Include(p => p.Equipe)
            .FirstOrDefaultAsync(p => p.Id == id
                                   && p.EstabelecimentoId == estabelecimentoId
                                   && p.DeletadoEm == null);

    public async Task Salvar(ProcedimentoCirurgico procedimento)
    {
        if (procedimento.Id == 0)
        {
            // Insert força SaveChanges para popular o Id (necessário para o caller usar
            // ProcedimentoIdCriado). UnitOfWorkFilter garante o COMMIT da transação.
            await _db.ProcedimentosCirurgicos.AddAsync(procedimento);
            await _db.SaveChangesAsync();
        }
        else
        {
            // EF rastreia mudanças via Include — não chamar Update para não marcar
            // todas as colunas como modificadas (geraria UPDATE redundante).
        }
    }
}
