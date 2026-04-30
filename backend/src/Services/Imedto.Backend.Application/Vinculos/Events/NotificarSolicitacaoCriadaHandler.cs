using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Vinculos.Events;

/// <summary>
/// Reage a <see cref="SolicitacaoVinculoCriadaEvent"/> notificando o dono do estabelecimento.
/// Mensagem genérica (sem nome do profissional ou e-mail) — LGPD: o destinatário verá detalhes
/// na tela "Solicitações recebidas".
///
/// Item 4.7: além da notificação in-app, envia email opcional ao dono caso ele tenha email
/// cadastrado. Falha de email NÃO bloqueia o handler.
/// </summary>
public class NotificarSolicitacaoCriadaHandler : IEventHandler<SolicitacaoVinculoCriadaEvent>
{
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly INotificacaoService _notificacoes;
    private readonly IEmailService _email;
    private readonly ILogger<NotificarSolicitacaoCriadaHandler> _logger;

    public NotificarSolicitacaoCriadaHandler(
        IEstabelecimentoRepository estabelecimentoRepo,
        IUsuarioRepository usuarioRepo,
        INotificacaoService notificacoes,
        IEmailService email,
        ILogger<NotificarSolicitacaoCriadaHandler> logger)
    {
        _estabelecimentoRepo = estabelecimentoRepo;
        _usuarioRepo = usuarioRepo;
        _notificacoes = notificacoes;
        _email = email;
        _logger = logger;
    }

    public async Task Handle(SolicitacaoVinculoCriadaEvent @event)
    {
        var estab = await _estabelecimentoRepo.ObterPorIdOuNulo(@event.EstabelecimentoId);
        if (estab is null) return;

        await _notificacoes.EnviarAsync(
            usuarioId: estab.DonoUsuarioId,
            estabelecimentoId: @event.EstabelecimentoId,
            titulo: "Nova solicitação de vínculo",
            mensagem: "Um profissional solicitou acesso ao seu estabelecimento. Acesse 'Solicitações recebidas' para revisar.",
            categoria: CategoriaNotificacao.Convite,
            linkAcao: "/solicitacoes-vinculo/recebidas");

        try
        {
            var dono = await _usuarioRepo.ObterPorIdOuNulo(estab.DonoUsuarioId);
            if (dono is null || string.IsNullOrWhiteSpace(dono.Email)) return;

            await _email.EnviarAsync(
                para: dono.Email,
                assunto: "Nova solicitação de vínculo no seu estabelecimento",
                corpoHtml: """
                    <p>Olá!</p>
                    <p>Um profissional solicitou acesso ao seu estabelecimento no Imedto.</p>
                    <p>Acesse <strong>Solicitações recebidas</strong> para revisar e responder.</p>
                    """,
                corpoTexto: "Um profissional solicitou acesso ao seu estabelecimento no Imedto. Acesse 'Solicitações recebidas' para revisar.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar email de solicitação de vínculo {Id}.", @event.SolicitacaoId);
        }
    }
}
