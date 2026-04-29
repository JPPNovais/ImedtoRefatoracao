using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Vinculos.Events;

/// <summary>
/// Reage a <see cref="SolicitacaoVinculoCriadaEvent"/> notificando o dono do estabelecimento.
/// Mensagem genérica (sem nome do profissional ou e-mail) — LGPD: o destinatário verá detalhes
/// na tela "Solicitações recebidas".
/// </summary>
public class NotificarSolicitacaoCriadaHandler : IEventHandler<SolicitacaoVinculoCriadaEvent>
{
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;
    private readonly INotificacaoService _notificacoes;

    public NotificarSolicitacaoCriadaHandler(
        IEstabelecimentoRepository estabelecimentoRepo,
        INotificacaoService notificacoes)
    {
        _estabelecimentoRepo = estabelecimentoRepo;
        _notificacoes = notificacoes;
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
    }
}
