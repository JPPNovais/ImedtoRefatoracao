using Imedto.Backend.Domain.Cirurgias.Events;
using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Cirurgias.Events;

/// <summary>
/// Reage ao <see cref="ProcedimentoConfirmadoEvent"/> e notifica cada membro
/// distinto da equipe (in-app via <see cref="INotificacaoService"/>).
///
/// Mensagem genérica — não inclui dados clínicos do paciente (LGPD), apenas o nome
/// da cirurgia principal. O profissional verá o detalhe ao abrir a cirurgia.
/// </summary>
public class NotificarEquipeAoConfirmarHandler : IEventHandler<ProcedimentoConfirmadoEvent>
{
    private readonly INotificacaoService _notificacoes;

    public NotificarEquipeAoConfirmarHandler(INotificacaoService notificacoes)
        => _notificacoes = notificacoes;

    public async Task Handle(ProcedimentoConfirmadoEvent domainEvent)
    {
        var quando = domainEvent.DataAgendada is { } d
            ? $" agendado para {d:dd/MM/yyyy HH:mm} (UTC)"
            : "";
        var mensagem = $"Você foi confirmado(a) para a cirurgia '{domainEvent.CirurgiaPrincipal}'{quando}.";

        foreach (var usuarioId in domainEvent.MembrosEquipeUsuarioIds.Distinct())
        {
            await _notificacoes.EnviarAsync(
                usuarioId: usuarioId,
                estabelecimentoId: domainEvent.EstabelecimentoId,
                titulo: "Cirurgia confirmada",
                mensagem: mensagem,
                categoria: CategoriaNotificacao.Sistema,
                linkAcao: $"/cirurgias/{domainEvent.ProcedimentoId}");
        }
    }
}
