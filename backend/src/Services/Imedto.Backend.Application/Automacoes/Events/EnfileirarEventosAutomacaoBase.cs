using System.Text.Json;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Infrastructure.Automacoes;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Automacoes.Events;

/// <summary>
/// Base para handlers que escutam um <see cref="IDomainEvent"/> concreto e enfileiram
/// <see cref="EventoAutomacao"/>s para cada regra ativa que tem este evento como gatilho.
///
/// Por que um handler por tipo de evento (em vez de <c>IEventHandler&lt;IDomainEvent&gt;</c>)?
/// O <c>MemoryEventBus</c> resolve handlers por tipo CONCRETO do evento — registrar como
/// genérico não dispatcharia para os concretos. Manter um handler por evento é mais explícito,
/// permite cada um declarar seu próprio nome de gatilho e evitar acoplar a lista de eventos
/// suportados em um único lugar.
/// </summary>
public abstract class EnfileirarEventosAutomacaoBase<TEvent> : IEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    private readonly IRegraAutomacaoRepository _regraRepo;
    private readonly IEventoAutomacaoRepository _eventoRepo;
    private readonly ILogger _logger;

    /// <summary>Nome canônico do gatilho — bate com <c>RegraAutomacao.EventoGatilho</c>.</summary>
    protected abstract string NomeGatilho { get; }

    /// <summary>Extrai o estabelecimento do payload — necessário para isolar regras por tenant.</summary>
    protected abstract long EstabelecimentoIdDoEvento(TEvent evento);

    protected EnfileirarEventosAutomacaoBase(
        IRegraAutomacaoRepository regraRepo,
        IEventoAutomacaoRepository eventoRepo,
        ILogger logger)
    {
        _regraRepo = regraRepo;
        _eventoRepo = eventoRepo;
        _logger = logger;
    }

    public async Task Handle(TEvent @event)
    {
        var estabelecimentoId = EstabelecimentoIdDoEvento(@event);
        if (estabelecimentoId <= 0) return;

        var regras = await _regraRepo.ListarAtivasPorEvento(estabelecimentoId, NomeGatilho);
        if (regras.Count == 0) return;

        // Serializa o payload uma vez — evita repetir por regra. JsonDocument readonly p/ avaliador.
        var payloadJson = JsonSerializer.Serialize<object>(@event);
        using var payloadDoc = JsonDocument.Parse(payloadJson);

        foreach (var regra in regras)
        {
            if (!AvaliadorCondicoes.Avaliar(regra.CondicoesJson, payloadDoc))
                continue;

            var evento = EventoAutomacao.Enfileirar(regra.Id, payloadJson);
            await _eventoRepo.Salvar(evento);

            _logger.LogInformation(
                "[Automacao] Evento {Gatilho} → enfileirado evento {EventoId} para regra {RegraId} (estab {Estab}).",
                NomeGatilho, evento.Id, regra.Id, estabelecimentoId);
        }
    }
}
