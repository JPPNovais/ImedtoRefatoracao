using Imedto.Backend.Contracts.Admin.Assinaturas.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Assinaturas;

public class SuspenderAssinaturaAdminCommandHandler
{
    private readonly IImedtoAssinaturaRepository _assinaturaRepo;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly IAssinaturaService _assinaturaService;
    private readonly AppDbContext _db;

    public SuspenderAssinaturaAdminCommandHandler(
        IImedtoAssinaturaRepository assinaturaRepo,
        ImedtoAdminAuditWriter audit,
        IAssinaturaService assinaturaService,
        AppDbContext db)
    {
        _assinaturaRepo = assinaturaRepo;
        _audit = audit;
        _assinaturaService = assinaturaService;
        _db = db;
    }

    public async Task Handle(SuspenderAssinaturaAdminCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Motivo))
            throw new BusinessException("Motivo e obrigatorio.");

        var vigente = await _assinaturaRepo.ObterVigenteDoEstabelecimentoAsync(cmd.EstabelecimentoId, ct)
            ?? throw new BusinessException("Estabelecimento nao possui assinatura vigente.");

        vigente.Suspender();
        _assinaturaRepo.Atualizar(vigente);
        await _db.SaveChangesAsync(ct);

        _assinaturaService.InvalidarCache(cmd.EstabelecimentoId);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.AlterarAssinatura,
            cmd.AdminId,
            recursoTipo: "assinatura",
            recursoId: vigente.Id.ToString(),
            tenantAfetadoId: cmd.EstabelecimentoId,
            motivo: cmd.Motivo,
            payloadJson: "{\"acao\":\"suspender\"}",
            ct: ct);
    }
}
