using Imedto.Backend.Contracts.Admin.Planos.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Planos;

public class DesativarPlanoAdminCommandHandler
{
    /// <summary>ID fixo do plano Gratuidade Vitalícia — não pode ser desativado nunca.</summary>
    private static readonly Guid _idGratuidadeVitalicia = new("00000000-0000-0000-0000-000000000001");

    private readonly IImedtoPlanoRepository _planoRepo;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public DesativarPlanoAdminCommandHandler(
        IImedtoPlanoRepository planoRepo,
        ImedtoAdminAuditWriter audit,
        AppDbContext db)
    {
        _planoRepo = planoRepo;
        _audit = audit;
        _db = db;
    }

    public async Task Handle(DesativarPlanoAdminCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Motivo))
            throw new BusinessException("Motivo é obrigatório.");

        if (cmd.PlanoId == _idGratuidadeVitalicia)
            throw new BusinessException("O plano Gratuidade Vitalícia não pode ser desativado.");

        var plano = await _planoRepo.ObterPorIdAsync(cmd.PlanoId, ct)
            ?? throw new BusinessException("Plano não encontrado.");

        plano.Inativar();

        _planoRepo.Atualizar(plano);
        await _db.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.DesativarPlano,
            cmd.AdminId,
            recursoTipo: "plano",
            recursoId: plano.Id.ToString(),
            motivo: cmd.Motivo,
            ct: ct);
    }
}
