using Imedto.Backend.Contracts.Notificacoes.Commands;
using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Notificacoes.Commands;

public class MarcarNotificacaoLidaCommandHandler : ICommandHandler<MarcarNotificacaoLidaCommand>
{
    private readonly INotificacaoRepository _repo;

    public MarcarNotificacaoLidaCommandHandler(INotificacaoRepository repo) => _repo = repo;

    public async Task Handle(MarcarNotificacaoLidaCommand command)
    {
        // Defense-in-depth IDOR: notificacoes sao per-usuario, filtro feito no proprio repo.
        var notif = await _repo.ObterPorIdOuNulo(command.NotificacaoId, command.UsuarioId)
            ?? throw new BusinessException("Notificação não encontrada.");

        notif.MarcarComoLida();
        await _repo.Salvar(notif);
    }
}
