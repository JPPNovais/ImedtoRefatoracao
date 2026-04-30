using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Vinculos.Events;

/// <summary>
/// Reage ao <see cref="ProfissionalConvidadoEvent"/> criando uma notificação in-app
/// para o profissional convidado. Esta é a ponte entre Vínculos × item 2.3 (Notificações).
///
/// Item 4.7: além da notificação in-app, envia email "você foi convidado" caso o
/// profissional tenha email cadastrado. Falha de email NÃO bloqueia o handler — a
/// notificação in-app é a fonte da verdade.
///
/// Mensagem genérica (não inclui o nome do estabelecimento) — LGPD/UX: o profissional
/// pode estar sendo convidado para algum lugar que ele ainda não conhece, e o nome do
/// estabelecimento aparece naturalmente na tela de convites.
/// </summary>
public class NotificarConviteAoConvidarProfissionalHandler : IEventHandler<ProfissionalConvidadoEvent>
{
    private readonly INotificacaoService _notificacoes;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IEmailService _email;
    private readonly ILogger<NotificarConviteAoConvidarProfissionalHandler> _logger;

    public NotificarConviteAoConvidarProfissionalHandler(
        INotificacaoService notificacoes,
        IUsuarioRepository usuarioRepo,
        IEmailService email,
        ILogger<NotificarConviteAoConvidarProfissionalHandler> logger)
    {
        _notificacoes = notificacoes;
        _usuarioRepo = usuarioRepo;
        _email = email;
        _logger = logger;
    }

    public async Task Handle(ProfissionalConvidadoEvent @event)
    {
        await _notificacoes.EnviarAsync(
            usuarioId: @event.ProfissionalUsuarioId,
            estabelecimentoId: null, // convite é "global do usuário" — vê em qualquer contexto
            titulo: "Você foi convidado",
            mensagem: "Você recebeu um convite para vincular-se a um estabelecimento. Acesse 'Meus convites' para revisar e aceitar.",
            categoria: CategoriaNotificacao.Convite,
            linkAcao: "/convites");

        // Email opcional — só dispara se o usuário tiver email cadastrado.
        try
        {
            var usuario = await _usuarioRepo.ObterPorIdOuNulo(@event.ProfissionalUsuarioId);
            if (usuario is null || string.IsNullOrWhiteSpace(usuario.Email)) return;

            await _email.EnviarAsync(
                para: usuario.Email,
                assunto: "Você foi convidado a um estabelecimento no Imedto",
                corpoHtml: """
                    <p>Olá!</p>
                    <p>Você recebeu um convite para se vincular a um estabelecimento no Imedto.</p>
                    <p>Acesse a aba <strong>Meus convites</strong> para revisar e aceitar.</p>
                    """,
                corpoTexto: "Você recebeu um convite para se vincular a um estabelecimento no Imedto. Acesse 'Meus convites' para revisar.");
        }
        catch (Exception ex)
        {
            // Não relançar — notificação in-app já foi entregue, email é canal complementar.
            _logger.LogWarning(ex, "Falha ao enviar email de convite para vínculo {VinculoId}.", @event.VinculoId);
        }
    }
}
