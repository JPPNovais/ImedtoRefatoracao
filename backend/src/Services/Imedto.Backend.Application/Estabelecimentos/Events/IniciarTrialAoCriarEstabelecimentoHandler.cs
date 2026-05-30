using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Estabelecimentos.Events;

/// <summary>
/// Reage a <see cref="EstabelecimentoCriadoEvent"/> criando uma assinatura em trial atrelada
/// ao plano "Trial" (semeado pelo <c>SeedPlanosHostedService</c>).
///
/// Duração do trial lida de <c>trial.dias_padrao</c> via <see cref="IConfigGlobalReader"/>
/// (fallback 14 dias — W2-CA8, W2-CA9). Não é retroativo: lê no momento da criação.
///
/// Falha controlada: se o plano "Trial" ainda não foi semeado, faz log e retorna sem trial.
/// </summary>
public class IniciarTrialAoCriarEstabelecimentoHandler : IEventHandler<EstabelecimentoCriadoEvent>
{
    private const string NomePlanoTrial = "Trial";
    private const int DuracaoTrialDefaultDias = 14;

    private readonly IPlanoRepository _planoRepo;
    private readonly IAssinaturaRepository _assinaturaRepo;
    private readonly IConfigGlobalReader _configReader;
    private readonly ILogger<IniciarTrialAoCriarEstabelecimentoHandler> _logger;

    public IniciarTrialAoCriarEstabelecimentoHandler(
        IPlanoRepository planoRepo,
        IAssinaturaRepository assinaturaRepo,
        IConfigGlobalReader configReader,
        ILogger<IniciarTrialAoCriarEstabelecimentoHandler> logger)
    {
        _planoRepo = planoRepo;
        _assinaturaRepo = assinaturaRepo;
        _configReader = configReader;
        _logger = logger;
    }

    public async Task Handle(EstabelecimentoCriadoEvent domainEvent)
    {
        // Idempotência: se já existe assinatura para este estabelecimento, ignora.
        var existente = await _assinaturaRepo.ObterPorEstabelecimentoOuNulo(domainEvent.EstabelecimentoId);
        if (existente is not null)
        {
            _logger.LogInformation(
                "Estabelecimento {EstabelecimentoId} já possui assinatura — pulando inicialização de trial.",
                domainEvent.EstabelecimentoId);
            return;
        }

        var planoTrial = await _planoRepo.ObterPorNomeOuNulo(NomePlanoTrial);
        if (planoTrial is null)
        {
            _logger.LogWarning(
                "Plano '{Nome}' não encontrado — trial não foi iniciado para o estabelecimento {EstabelecimentoId}. "
                + "Verifique se o SeedPlanosHostedService rodou.",
                NomePlanoTrial, domainEvent.EstabelecimentoId);
            return;
        }

        // Lê dias do trial da config global; fallback = 14 (R6: só afeta novos estabelecimentos).
        var diasTrial = await _configReader.LerInt("trial.dias_padrao", DuracaoTrialDefaultDias);
        if (diasTrial <= 0) diasTrial = DuracaoTrialDefaultDias;

        var duracao = TimeSpan.FromDays(diasTrial);
        var assinatura = Assinatura.IniciarTrial(domainEvent.EstabelecimentoId, planoTrial.Id, duracao);
        await _assinaturaRepo.Salvar(assinatura);

        _logger.LogInformation(
            "Trial iniciado: Assinatura={AssinaturaId} Estabelecimento={EstabelecimentoId} Dias={Dias} ExpiraEm={ExpiraEm:o}",
            assinatura.Id, domainEvent.EstabelecimentoId, diasTrial, assinatura.ExpiraEm);
    }
}
