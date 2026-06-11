using Imedto.Backend.Contracts.Admin.Assinaturas.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Assinaturas;

public class IniciarTrialAdminCommandHandler
{
    private readonly IImedtoAssinaturaRepository _assinaturaRepo;
    private readonly IImedtoPlanoRepository _planoRepo;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly IAssinaturaService _assinaturaService;
    private readonly AppDbContext _db;

    public IniciarTrialAdminCommandHandler(
        IImedtoAssinaturaRepository assinaturaRepo,
        IImedtoPlanoRepository planoRepo,
        ImedtoAdminAuditWriter audit,
        IAssinaturaService assinaturaService,
        AppDbContext db)
    {
        _assinaturaRepo = assinaturaRepo;
        _planoRepo = planoRepo;
        _audit = audit;
        _assinaturaService = assinaturaService;
        _db = db;
    }

    public async Task Handle(IniciarTrialAdminCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Motivo))
            throw new BusinessException("Motivo e obrigatorio.");

        if (cmd.Dias <= 0)
            throw new BusinessException("Duracao do trial deve ser maior que zero.");

        var plano = await _planoRepo.ObterPorIdAsync(cmd.PlanoId, ct)
            ?? throw new BusinessException("Plano de trial nao encontrado.");

        if (!plano.Ativo)
            throw new BusinessException("Nao e possivel iniciar trial com um plano inativo.");

        var expiraEm = DateTimeOffset.UtcNow.AddDays(cmd.Dias);

        var vigente = await _assinaturaRepo.ObterVigenteDoEstabelecimentoAsync(cmd.EstabelecimentoId, ct);
        if (vigente is not null)
        {
            vigente.FecharVigencia();
            _assinaturaRepo.Atualizar(vigente);
        }

        var nova = ImedtoAssinatura.Criar(
            cmd.EstabelecimentoId,
            cmd.PlanoId,
            gratuita: false,
            motivo: cmd.Motivo,
            criadaPorAdminId: cmd.AdminId,
            expiraEm: expiraEm);

        _assinaturaRepo.Adicionar(nova);
        await _db.SaveChangesAsync(ct);

        _assinaturaService.InvalidarCache(cmd.EstabelecimentoId);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.AlterarAssinatura,
            cmd.AdminId,
            recursoTipo: "assinatura",
            recursoId: nova.Id.ToString(),
            tenantAfetadoId: cmd.EstabelecimentoId,
            motivo: cmd.Motivo,
            payloadJson: $"{{\"acao\":\"iniciar_trial\",\"dias\":{cmd.Dias},\"plano\":\"{plano.Nome}\",\"expira_em\":\"{expiraEm:O}\"}}",
            ct: ct);
    }
}
