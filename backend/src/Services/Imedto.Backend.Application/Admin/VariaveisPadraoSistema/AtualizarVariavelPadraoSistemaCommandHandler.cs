using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Application.Admin.VariaveisPadraoSistema;

public class AtualizarVariavelPadraoSistemaCommandHandler
{
    private readonly VariavelPadraoSistemaQueryRepository _query;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public AtualizarVariavelPadraoSistemaCommandHandler(
        VariavelPadraoSistemaQueryRepository query,
        ImedtoAdminAuditWriter audit,
        AppDbContext db)
    {
        _query = query;
        _audit = audit;
        _db = db;
    }

    public async Task Handle(AtualizarVariavelPadraoSistemaCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Informe o motivo da alteração (mínimo 10 caracteres).");

        if (!Enum.TryParse<TipoVariavelPool>(command.Tipo, ignoreCase: true, out _))
            throw new BusinessException("Categoria inválida.");

        var variavel = await _db.ProntuarioVariaveisPool
            .FirstOrDefaultAsync(v => v.Id == command.Id && v.EhPadraoSistema, ct);
        if (variavel is null)
            throw new BusinessException("Variável não encontrada.");

        if (await _query.ExisteNomePorCategoriaParaSistema(command.Nome, command.Tipo, ignorarId: command.Id, ct: ct))
            throw new BusinessException("Já existe variável padrão do sistema com esse nome nessa categoria.");

        variavel.Renomear(command.Nome);
        _db.ProntuarioVariaveisPool.Update(variavel);
        await _db.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.AtualizarVariavelPadraoSistema,
            command.AdminId,
            recursoTipo: "variavel_pool",
            recursoId: variavel.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);
    }
}

public record AtualizarVariavelPadraoSistemaCommand(
    long Id,
    string Nome,
    string Tipo,
    string Motivo,
    Guid? AdminId);
