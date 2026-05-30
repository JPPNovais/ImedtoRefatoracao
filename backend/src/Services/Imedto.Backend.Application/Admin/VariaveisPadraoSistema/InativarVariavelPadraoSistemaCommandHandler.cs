using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Application.Admin.VariaveisPadraoSistema;

public class InativarVariavelPadraoSistemaCommandHandler
{
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public InativarVariavelPadraoSistemaCommandHandler(ImedtoAdminAuditWriter audit, AppDbContext db)
    {
        _audit = audit;
        _db = db;
    }

    public async Task Handle(InativarVariavelPadraoSistemaCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Informe o motivo da alteração (mínimo 10 caracteres).");

        var variavel = await _db.ProntuarioVariaveisPool
            .FirstOrDefaultAsync(v => v.Id == command.Id && v.EhPadraoSistema, ct);
        if (variavel is null)
            throw new BusinessException("Variável não encontrada.");

        variavel.Inativar();
        _db.ProntuarioVariaveisPool.Update(variavel);
        await _db.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.InativarVariavelPadraoSistema,
            command.AdminId,
            recursoTipo: "variavel_pool",
            recursoId: variavel.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);
    }
}

public record InativarVariavelPadraoSistemaCommand(long Id, string Motivo, Guid? AdminId);
