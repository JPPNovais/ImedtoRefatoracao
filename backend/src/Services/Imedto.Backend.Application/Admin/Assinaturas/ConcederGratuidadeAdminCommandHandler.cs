using Imedto.Backend.Contracts.Admin.Assinaturas.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Assinaturas;

public class ConcederGratuidadeAdminCommandHandler
{
    /// <summary>ID fixo do plano Gratuidade Vitalícia seedado.</summary>
    private static readonly Guid _idGratuidadeVitalicia = new("00000000-0000-0000-0000-000000000001");

    private readonly IImedtoAssinaturaRepository _assinaturaRepo;
    private readonly IImedtoPlanoRepository _planoRepo;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public ConcederGratuidadeAdminCommandHandler(
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

    public async Task Handle(ConcederGratuidadeAdminCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Motivo) || cmd.Motivo.Trim().Length < 10)
            throw new BusinessException("Motivo é obrigatório para concessão de gratuidade (mínimo 10 caracteres).");

        if (string.IsNullOrWhiteSpace(cmd.GratuidadeMotivo) || cmd.GratuidadeMotivo.Trim().Length < 10)
            throw new BusinessException("Motivo é obrigatório para concessão de gratuidade (mínimo 10 caracteres).");

        var planoGratuidade = await _planoRepo.ObterPorIdAsync(_idGratuidadeVitalicia, ct)
            ?? throw new BusinessException("Plano Gratuidade Vitalícia não encontrado. Verifique o seed.");

        // Fecha a vigente antes de criar a nova (mesma transação).
        var vigente = await _assinaturaRepo.ObterVigenteDoEstabelecimentoAsync(cmd.EstabelecimentoId, ct);
        if (vigente is not null)
        {
            vigente.FecharVigencia();
            _assinaturaRepo.Atualizar(vigente);
        }

        // Cria nova assinatura de gratuidade: motivo = gratuidade_motivo (armazenado no campo Motivo da linha).
        var nova = ImedtoAssinatura.Criar(
            cmd.EstabelecimentoId,
            _idGratuidadeVitalicia,
            gratuita: true,
            motivo: cmd.GratuidadeMotivo.Trim(),
            criadaPorAdminId: cmd.AdminId);

        _assinaturaRepo.Adicionar(nova);
        await _db.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.ConcederGratuidade,
            cmd.AdminId,
            recursoTipo: "assinatura",
            recursoId: nova.Id.ToString(),
            tenantAfetadoId: cmd.EstabelecimentoId,
            motivo: cmd.Motivo,
            ct: ct);
    }
}
