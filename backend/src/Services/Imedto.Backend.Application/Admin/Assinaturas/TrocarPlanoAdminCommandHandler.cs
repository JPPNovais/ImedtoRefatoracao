using Imedto.Backend.Contracts.Admin.Assinaturas.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Assinaturas;

public class TrocarPlanoAdminCommandHandler
{
    private readonly IImedtoAssinaturaRepository _assinaturaRepo;
    private readonly IImedtoPlanoRepository _planoRepo;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public TrocarPlanoAdminCommandHandler(
        IImedtoAssinaturaRepository assinaturaRepo,
        IImedtoPlanoRepository planoRepo,
        ImedtoAdminAuditWriter audit,
        AppDbContext db)
    {
        _assinaturaRepo = assinaturaRepo;
        _planoRepo = planoRepo;
        _audit = audit;
        _db = db;
    }

    public async Task Handle(TrocarPlanoAdminCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Motivo))
            throw new BusinessException("Motivo é obrigatório.");

        var plano = await _planoRepo.ObterPorIdAsync(cmd.PlanoId, ct)
            ?? throw new BusinessException("Plano não encontrado.");

        if (!plano.Ativo)
            throw new BusinessException("Não é possível atribuir um plano inativo.");

        // Troca de plano: INSERT nova linha + FecharVigencia da anterior — mesma transação.
        var vigente = await _assinaturaRepo.ObterVigenteDoEstabelecimentoAsync(cmd.EstabelecimentoId, ct);
        var planoAntigoNome = vigente is not null ? "desconhecido" : null;

        string? payloadJson = null;
        if (vigente is not null)
        {
            vigente.FecharVigencia();
            _assinaturaRepo.Atualizar(vigente);

            // Buscar nome do plano anterior para o payload de audit (sem PII).
            var planoAntigo = await _planoRepo.ObterPorIdAsync(vigente.PlanoId, ct);
            payloadJson = $"{{\"plano_antigo\":\"{planoAntigo?.Nome ?? vigente.PlanoId.ToString()}\",\"plano_novo\":\"{plano.Nome}\"}}";
        }

        var nova = ImedtoAssinatura.Criar(
            cmd.EstabelecimentoId,
            cmd.PlanoId,
            gratuita: false,
            motivo: cmd.Motivo,
            criadaPorAdminId: cmd.AdminId);

        _assinaturaRepo.Adicionar(nova);
        await _db.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.TrocarPlano,
            cmd.AdminId,
            recursoTipo: "assinatura",
            recursoId: nova.Id.ToString(),
            tenantAfetadoId: cmd.EstabelecimentoId,
            motivo: cmd.Motivo,
            payloadJson: payloadJson,
            ct: ct);
    }
}
