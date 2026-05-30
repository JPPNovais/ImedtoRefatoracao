using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.ModelosPadraoSistema;

public class ReativarModeloPadraoSistemaCommandHandler
{
    private readonly IModeloDeProntuarioRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public ReativarModeloPadraoSistemaCommandHandler(
        IModeloDeProntuarioRepository repo,
        ImedtoAdminAuditWriter audit,
        AppDbContext db)
    {
        _repo = repo;
        _audit = audit;
        _db = db;
    }

    public async Task Handle(ReativarModeloPadraoSistemaCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Informe o motivo da alteração (mínimo 10 caracteres).");

        var modelo = await _repo.ObterVisivelOuNulo(command.Id, 0);
        if (modelo is null || !modelo.EhPadraoSistema)
            throw new BusinessException("Modelo não encontrado.");

        modelo.Reativar();
        _db.ModelosDeProntuario.Update(modelo);
        await _db.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.ReativarModeloPadraoSistema,
            command.AdminId,
            recursoTipo: "modelo_prontuario",
            recursoId: modelo.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);
    }
}

public record ReativarModeloPadraoSistemaCommand(long Id, string Motivo, Guid? AdminId);
