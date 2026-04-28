using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ProntuarioAcessoLogService : IProntuarioAcessoLogService
{
    private readonly AppDbContext _context;

    public ProntuarioAcessoLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarAsync(
        long prontuarioId,
        Guid usuarioId,
        long estabelecimentoId,
        TipoAcessoProntuario tipoAcesso)
    {
        var log = ProntuarioAcessoLog.Registrar(prontuarioId, usuarioId, estabelecimentoId, tipoAcesso);
        await _context.ProntuarioAcessoLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }
}
