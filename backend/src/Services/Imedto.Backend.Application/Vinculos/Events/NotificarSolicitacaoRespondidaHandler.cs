using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Vinculos.Events;

/// <summary>
/// Notifica o profissional sobre a resposta (aprovada ou recusada) à sua solicitação de vínculo.
/// Implementa dois <c>IEventHandler</c> em uma classe — registrados separadamente no bus.
/// </summary>
public class NotificarSolicitacaoRespondidaHandler
    : IEventHandler<SolicitacaoVinculoAprovadaEvent>,
      IEventHandler<SolicitacaoVinculoRecusadaEvent>
{
    private readonly INotificacaoService _notificacoes;

    public NotificarSolicitacaoRespondidaHandler(INotificacaoService notificacoes)
    {
        _notificacoes = notificacoes;
    }

    public Task Handle(SolicitacaoVinculoAprovadaEvent @event)
        => _notificacoes.EnviarAsync(
            usuarioId: @event.ProfissionalUsuarioId,
            estabelecimentoId: null, // global do usuário — vê em qualquer contexto
            titulo: "Solicitação de vínculo aprovada",
            mensagem: "Sua solicitação foi aprovada. Você já pode atuar no estabelecimento.",
            categoria: CategoriaNotificacao.Convite,
            linkAcao: "/solicitacoes-vinculo/minhas");

    public Task Handle(SolicitacaoVinculoRecusadaEvent @event)
        => _notificacoes.EnviarAsync(
            usuarioId: @event.ProfissionalUsuarioId,
            estabelecimentoId: null,
            titulo: "Solicitação de vínculo recusada",
            mensagem: "Sua solicitação de vínculo foi recusada. Acesse 'Minhas solicitações' para mais detalhes.",
            categoria: CategoriaNotificacao.Convite,
            linkAcao: "/solicitacoes-vinculo/minhas");
}
