using Imedto.Backend.Contracts.Admin.Assinaturas.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Assinaturas;

public class EncerrarAssinaturaAdminCommandHandler
{
    private readonly IImedtoAssinaturaRepository _assinaturaRepo;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public EncerrarAssinaturaAdminCommandHandler(
        IImedtoAssinaturaRepository assinaturaRepo,
        ImedtoAdminAuditWriter audit,
        AppDbContext db)
    {
        _assinaturaRepo = assinaturaRepo;
        _audit = audit;
        _db = db;
    }

    public async Task Handle(EncerrarAssinaturaAdminCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Motivo))
            throw new BusinessException("Motivo é obrigatório.");

        var assinatura = await _assinaturaRepo.ObterPorIdAsync(cmd.AssinaturaId, ct)
            ?? throw new BusinessException("Assinatura não encontrada.");

        // FecharVigencia() lança BusinessException se já encerrada (invariante do domínio).
        assinatura.FecharVigencia();

        _assinaturaRepo.Atualizar(assinatura);
        await _db.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.EncerrarAssinatura,
            cmd.AdminId,
            recursoTipo: "assinatura",
            recursoId: cmd.AssinaturaId.ToString(),
            tenantAfetadoId: assinatura.EstabelecimentoId,
            motivo: cmd.Motivo,
            ct: ct);
    }
}
