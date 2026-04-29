using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Notificacoes;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class NotificacaoRepository : INotificacaoRepository
{
    private readonly AppDbContext _db;

    public NotificacaoRepository(AppDbContext db) => _db = db;

    public async Task<Notificacao?> ObterPorIdOuNulo(long id)
        => await _db.Notificacoes.FirstOrDefaultAsync(n => n.Id == id);

    public async Task Salvar(Notificacao notificacao)
    {
        if (notificacao.Id == 0)
            _db.Notificacoes.Add(notificacao);
        else
            _db.Notificacoes.Update(notificacao);
        await _db.SaveChangesAsync();
    }

    public async Task<int> MarcarTodasLidasDoUsuario(Guid usuarioId)
    {
        var agora = DateTime.UtcNow;
        // ExecuteUpdate evita carregar todas as linhas em memória — vai como UPDATE atômico.
        return await _db.Notificacoes
            .Where(n => n.UsuarioId == usuarioId && !n.Lida)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.Lida, true)
                .SetProperty(n => n.LidaEm, agora));
    }
}
