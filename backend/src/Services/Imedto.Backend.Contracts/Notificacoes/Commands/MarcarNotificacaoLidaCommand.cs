using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Notificacoes.Commands;

public class MarcarNotificacaoLidaCommand : ICommand
{
    public long NotificacaoId { get; set; }
    public Guid UsuarioId { get; set; }
}

public class MarcarTodasNotificacoesLidasCommand : ICommand
{
    public Guid UsuarioId { get; set; }
}
