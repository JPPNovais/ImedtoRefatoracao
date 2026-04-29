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
        var notif = await _repo.ObterPorIdOuNulo(command.NotificacaoId)
            ?? throw new BusinessException("Notificação não encontrada.");

        // Mensagem genérica para evitar enumeração: mesmo erro para "não existe" e "não é sua".
        if (notif.UsuarioId != command.UsuarioId)
            throw new BusinessException("Notificação não encontrada.");

        notif.MarcarComoLida();
        await _repo.Salvar(notif);
    }
}
