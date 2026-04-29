using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Vinculos.Events;

/// <summary>
/// Reage ao <see cref="ProfissionalConvidadoEvent"/> criando uma notificação in-app
/// para o profissional convidado. Esta é a ponte entre Vínculos × item 2.3 (Notificações).
///
/// Mensagem genérica (não inclui o nome do estabelecimento) — LGPD/UX: o profissional
/// pode estar sendo convidado para algum lugar que ele ainda não conhece, e o nome do
/// estabelecimento aparece naturalmente na tela de convites.
/// </summary>
public class NotificarConviteAoConvidarProfissionalHandler : IEventHandler<ProfissionalConvidadoEvent>
{
    private readonly INotificacaoService _notificacoes;

    public NotificarConviteAoConvidarProfissionalHandler(INotificacaoService notificacoes)
        => _notificacoes = notificacoes;

    public Task Handle(ProfissionalConvidadoEvent @event)
        => _notificacoes.EnviarAsync(
            usuarioId: @event.ProfissionalUsuarioId,
            estabelecimentoId: null, // convite é "global do usuário" — vê em qualquer contexto
            titulo: "Você foi convidado",
            mensagem: "Você recebeu um convite para vincular-se a um estabelecimento. Acesse 'Meus convites' para revisar e aceitar.",
            categoria: CategoriaNotificacao.Convite,
            linkAcao: "/convites");
}
