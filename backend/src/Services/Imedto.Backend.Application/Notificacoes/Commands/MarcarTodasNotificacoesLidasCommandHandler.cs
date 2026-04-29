using Imedto.Backend.Contracts.Notificacoes.Commands;
using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Notificacoes.Commands;

public class MarcarTodasNotificacoesLidasCommandHandler : ICommandHandler<MarcarTodasNotificacoesLidasCommand>
{
    private readonly INotificacaoRepository _repo;

    public MarcarTodasNotificacoesLidasCommandHandler(INotificacaoRepository repo) => _repo = repo;

    public Task Handle(MarcarTodasNotificacoesLidasCommand command)
        => _repo.MarcarTodasLidasDoUsuario(command.UsuarioId);
}
