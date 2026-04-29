using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Domain.Notificacoes;

namespace Imedto.Backend.Infrastructure.Assinaturas.Handlers;

/// <summary>
/// Expira trials que já passaram da data de <c>expira_em</c>. Roda 1x/h (registrado em
/// <c>JobsRegistrados</c>). Idempotente — se a assinatura já está em <see cref="StatusAssinatura.Expirada"/>,
/// o aggregate ignora a transição (evita N+1 commits ruidosos).
///
/// Para cada trial expirado, dispara notificação in-app para o dono do estabelecimento,
/// linkando para <c>/minha-assinatura</c> onde ele vai escolher o novo plano. A notificação
/// é mandatória mas isolada do commit do aggregate — falha em notificar não impede a expiração
/// (a notificação pode ser reemitida via reprocesso).
/// </summary>
public class ExpirarTrialsJob : IJobHandler
{
    public string Nome => "expirar-trials";

    private readonly IAssinaturaRepository _assinaturaRepo;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;
    private readonly INotificacaoService _notificacaoService;
    private readonly ILogger<ExpirarTrialsJob> _logger;

    public ExpirarTrialsJob(
        IAssinaturaRepository assinaturaRepo,
        IEstabelecimentoRepository estabelecimentoRepo,
        INotificacaoService notificacaoService,
        ILogger<ExpirarTrialsJob> logger)
    {
        _assinaturaRepo = assinaturaRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
        _notificacaoService = notificacaoService;
        _logger = logger;
    }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        var trials = await _assinaturaRepo.ListarTrialsExpirando(agora);

        if (trials.Count == 0)
        {
            _logger.LogDebug("[Job:{Nome}] Nenhum trial elegível para expirar.", Nome);
            return;
        }

        _logger.LogInformation("[Job:{Nome}] {Quantidade} trial(s) elegíveis para expiração.", Nome, trials.Count);

        var expirados = 0;
        foreach (var assinatura in trials)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                assinatura.Expirar();
                await _assinaturaRepo.Salvar(assinatura);
                expirados++;

                await NotificarDono(assinatura, ct);
            }
            catch (Exception ex)
            {
                // Falha em uma assinatura específica não pode interromper as demais — registra
                // e segue. Próximo tick re-pega (idempotência via aggregate.Expirar).
                _logger.LogError(ex,
                    "[Job:{Nome}] Falha ao expirar trial {AssinaturaId} (estabelecimento {EstabelecimentoId}).",
                    Nome, assinatura.Id, assinatura.EstabelecimentoId);
            }
        }

        _logger.LogInformation("[Job:{Nome}] {Expirados}/{Total} trial(s) expirados nesta rodada.",
            Nome, expirados, trials.Count);
    }

    private async Task NotificarDono(Assinatura assinatura, CancellationToken ct)
    {
        try
        {
            var estabelecimento = await _estabelecimentoRepo.ObterPorIdOuNulo(assinatura.EstabelecimentoId);
            if (estabelecimento is null)
            {
                _logger.LogWarning(
                    "[Job:{Nome}] Estabelecimento {EstabelecimentoId} não encontrado — sem notificação.",
                    Nome, assinatura.EstabelecimentoId);
                return;
            }

            await _notificacaoService.EnviarAsync(
                usuarioId: estabelecimento.DonoUsuarioId,
                estabelecimentoId: estabelecimento.Id,
                titulo: "Seu período de trial expirou",
                mensagem: "Para continuar usando todos os recursos da plataforma, escolha um plano e mantenha sua assinatura ativa.",
                categoria: CategoriaNotificacao.Sistema,
                linkAcao: "/minha-assinatura",
                ct: ct);
        }
        catch (Exception ex)
        {
            // Não-crítico: a expiração já foi persistida; a notificação é melhor-esforço.
            _logger.LogError(ex,
                "[Job:{Nome}] Falha ao notificar expiração do trial {AssinaturaId}.",
                Nome, assinatura.Id);
        }
    }
}
