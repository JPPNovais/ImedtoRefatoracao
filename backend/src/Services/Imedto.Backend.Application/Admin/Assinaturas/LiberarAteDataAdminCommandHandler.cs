using Imedto.Backend.Contracts.Admin.Assinaturas.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Assinaturas;

public class LiberarAteDataAdminCommandHandler
{
    private readonly IImedtoAssinaturaRepository _assinaturaRepo;
    private readonly IImedtoPlanoRepository _planoRepo;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly IAssinaturaService _assinaturaService;
    private readonly AppDbContext _db;

    public LiberarAteDataAdminCommandHandler(
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

    public async Task Handle(LiberarAteDataAdminCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Motivo))
            throw new BusinessException("Motivo e obrigatorio.");

        if (cmd.DataExpiracao <= DateTimeOffset.UtcNow)
            throw new BusinessException("A data de expiracao deve ser no futuro.");

        var plano = await _planoRepo.ObterPorIdAsync(cmd.PlanoId, ct)
            ?? throw new BusinessException("Plano nao encontrado.");

        if (!plano.Ativo)
            throw new BusinessException("Nao e possivel atribuir um plano inativo.");

        var vigente = await _assinaturaRepo.ObterVigenteDoEstabelecimentoAsync(cmd.EstabelecimentoId, ct);
        string? payloadJson = null;
        if (vigente is not null)
        {
            var planoAntigo = await _planoRepo.ObterPorIdAsync(vigente.PlanoId, ct);
            payloadJson = $"{{\"acao\":\"liberar_ate_data\",\"plano_anterior\":\"{planoAntigo?.Nome ?? vigente.PlanoId.ToString()}\",\"plano_novo\":\"{plano.Nome}\",\"expira_em\":\"{cmd.DataExpiracao:O}\"}}";
            vigente.FecharVigencia();
            _assinaturaRepo.Atualizar(vigente);
        }

        var nova = ImedtoAssinatura.Criar(
            cmd.EstabelecimentoId,
            cmd.PlanoId,
            gratuita: false,
            motivo: cmd.Motivo,
            criadaPorAdminId: cmd.AdminId,
            expiraEm: cmd.DataExpiracao);

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
            payloadJson: payloadJson ?? $"{{\"acao\":\"liberar_ate_data\",\"plano_novo\":\"{plano.Nome}\"}}",
            ct: ct);
    }
}
