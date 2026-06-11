using Imedto.Backend.Contracts.Admin.Planos.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Planos;

public class CriarPlanoAdminCommandHandler
{
    private readonly IImedtoPlanoRepository _planoRepo;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public CriarPlanoAdminCommandHandler(
        IImedtoPlanoRepository planoRepo,
        ImedtoAdminAuditWriter audit,
        AppDbContext db)
    {
        _planoRepo = planoRepo;
        _audit = audit;
        _db = db;
    }

    public async Task Handle(CriarPlanoAdminCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Motivo))
            throw new BusinessException("Motivo é obrigatório.");

        if (await _planoRepo.ExisteNomeAsync(cmd.Nome, excluindoId: null, ct))
            throw new BusinessException("Já existe um plano com este nome.");

        var plano = ImedtoPlano.Criar(
            cmd.Nome,
            cmd.DescricaoCurta,
            cmd.PrecoMensalCentavos,
            cmd.Gratuito,
            cmd.LimitesJson,
            cmd.AdminId,
            cmd.FeaturesJson);

        _planoRepo.Adicionar(plano);
        await _db.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.CriarPlano,
            cmd.AdminId,
            recursoTipo: "plano",
            recursoId: plano.Id.ToString(),
            motivo: cmd.Motivo,
            ct: ct);
    }
}
