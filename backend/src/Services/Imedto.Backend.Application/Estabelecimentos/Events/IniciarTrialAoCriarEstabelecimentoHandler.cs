using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Estabelecimentos.Events;

/// <summary>
/// Reage a <see cref="EstabelecimentoCriadoEvent"/> criando uma assinatura em trial atrelada
/// ao plano "Trial" (semeado pelo <c>SeedPlanosHostedService</c>). Duração default de 14 dias —
/// alinhado ao item 2.7 da Fase 2.
///
/// Falha controlada: se o plano "Trial" ainda não foi semeado (raríssimo, só na primeira request
/// após deploy antes do hosted service rodar), faz log e retorna sem trial — o estabelecimento
/// ainda funciona em features core, e o trial pode ser ativado depois manualmente. Não fazer
/// throw evita corromper a transação do <c>UnitOfWorkAttribute</c>.
/// </summary>
public class IniciarTrialAoCriarEstabelecimentoHandler : IEventHandler<EstabelecimentoCriadoEvent>
{
    private const string NomePlanoTrial = "Trial";
    private static readonly TimeSpan DuracaoTrialPadrao = TimeSpan.FromDays(14);

    private readonly IPlanoRepository _planoRepo;
    private readonly IAssinaturaRepository _assinaturaRepo;
    private readonly ILogger<IniciarTrialAoCriarEstabelecimentoHandler> _logger;

    public IniciarTrialAoCriarEstabelecimentoHandler(
        IPlanoRepository planoRepo,
        IAssinaturaRepository assinaturaRepo,
        ILogger<IniciarTrialAoCriarEstabelecimentoHandler> logger)
    {
        _planoRepo = planoRepo;
        _assinaturaRepo = assinaturaRepo;
        _logger = logger;
    }

    public async Task Handle(EstabelecimentoCriadoEvent @event)
    {
        // Idempotência: se já existe assinatura para este estabelecimento, ignora —
        // re-execuções de evento (retry) não devem criar trials duplicados.
        var existente = await _assinaturaRepo.ObterPorEstabelecimentoOuNulo(@event.EstabelecimentoId);
        if (existente is not null)
        {
            _logger.LogInformation(
                "Estabelecimento {EstabelecimentoId} já possui assinatura — pulando inicialização de trial.",
                @event.EstabelecimentoId);
            return;
        }

        var planoTrial = await _planoRepo.ObterPorNomeOuNulo(NomePlanoTrial);
        if (planoTrial is null)
        {
            _logger.LogWarning(
                "Plano '{Nome}' não encontrado — trial não foi iniciado para o estabelecimento {EstabelecimentoId}. "
                + "Verifique se o SeedPlanosHostedService rodou.",
                NomePlanoTrial, @event.EstabelecimentoId);
            return;
        }

        var assinatura = Assinatura.IniciarTrial(@event.EstabelecimentoId, planoTrial.Id, DuracaoTrialPadrao);
        await _assinaturaRepo.Salvar(assinatura);

        _logger.LogInformation(
            "Trial iniciado: Assinatura={AssinaturaId} Estabelecimento={EstabelecimentoId} ExpiraEm={ExpiraEm:o}",
            assinatura.Id, @event.EstabelecimentoId, assinatura.ExpiraEm);
    }
}
