using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Estabelecimentos.Events;

/// <summary>
/// Reage a <see cref="EstabelecimentoCriadoEvent"/> criando uma assinatura de trial na estrutura
/// nova (<c>imedto_assinaturas</c>) usando a config global editável (<c>imedto_config_trial</c>).
///
/// R7 do briefing 2026-06-11_003 (F5):
/// - trial_habilitado=true  → cria ImedtoAssinatura vigente com expira_em=now+duracao_trial_dias.
/// - trial_habilitado=false → estabelecimento nasce sem assinatura vigente (estado BLOQUEADO).
///
/// Atomicidade: o handler roda dentro do mesmo DbContext da request (scope da transação do
/// CriarEstabelecimentoCommandHandler, via MemoryEventBus síncrono). O SaveChanges final
/// inclui tanto o estabelecimento quanto a assinatura no mesmo commit.
///
/// NOTA: NÃO escreve na estrutura legada (assinaturas). F6 (2026-06-11_003) removeu
/// IAssinaturaRepository e ExpirarTrialsJob — estrutura legada é read-only, drop físico posterior.
/// </summary>
public class IniciarTrialAoCriarEstabelecimentoHandler : IEventHandler<EstabelecimentoCriadoEvent>
{
    private readonly IImedtoConfigTrialRepository _configTrialRepo;
    private readonly IImedtoAssinaturaRepository _assinaturaRepo;
    private readonly AppDbContext _db;
    private readonly ILogger<IniciarTrialAoCriarEstabelecimentoHandler> _logger;

    public IniciarTrialAoCriarEstabelecimentoHandler(
        IImedtoConfigTrialRepository configTrialRepo,
        IImedtoAssinaturaRepository assinaturaRepo,
        AppDbContext db,
        ILogger<IniciarTrialAoCriarEstabelecimentoHandler> logger)
    {
        _configTrialRepo = configTrialRepo;
        _assinaturaRepo = assinaturaRepo;
        _db = db;
        _logger = logger;
    }

    public async Task Handle(EstabelecimentoCriadoEvent domainEvent)
    {
        var config = await _configTrialRepo.ObterAsync();
        if (config is null)
        {
            // Config ainda não semeada (ambiente de teste ou startup incompleto).
            _logger.LogWarning(
                "Config de trial não encontrada — estabelecimento {EstabelecimentoId} nasce sem assinatura vigente.",
                domainEvent.EstabelecimentoId);
            return;
        }

        if (!config.TrialHabilitado)
        {
            // R7: trial desligado → nasce bloqueado (sem vigência).
            _logger.LogInformation(
                "Trial desabilitado — estabelecimento {EstabelecimentoId} nasce sem assinatura vigente (BLOQUEADO).",
                domainEvent.EstabelecimentoId);
            return;
        }

        var expiraEm = DateTimeOffset.UtcNow.AddDays(config.DuracaoTrialDias);

        var assinatura = ImedtoAssinatura.Criar(
            estabelecimentoId: domainEvent.EstabelecimentoId,
            planoId: config.PlanoTrialId,
            gratuita: false,
            motivo: null,
            criadaPorAdminId: null,
            expiraEm: expiraEm);

        _assinaturaRepo.Adicionar(assinatura);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Trial iniciado (nova estrutura): AssinaturaId={AssinaturaId} Estabelecimento={EstabelecimentoId} Dias={Dias} ExpiraEm={ExpiraEm:o}",
            assinatura.Id, domainEvent.EstabelecimentoId, config.DuracaoTrialDias, expiraEm);
    }
}
