using Imedto.Backend.Contracts.Notificacoes.Queries;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Notificacoes.Queries;

public class ListarNotificacoesQueryHandlers : IRequestHandler<ListarNotificacoesQuery, PaginaNotificacoesDto>
{
    private readonly NotificacaoQueryRepository _repo;

    public ListarNotificacoesQueryHandlers(NotificacaoQueryRepository repo) => _repo = repo;

    public Task<PaginaNotificacoesDto> Handle(ListarNotificacoesQuery query)
        => _repo.Listar(query.UsuarioId, query.Lidas, query.Pagina, query.Tamanho);
}

public class ContadorNaoLidasQueryHandlers : IRequestHandler<ContadorNaoLidasQuery, ContadorNaoLidasDto>
{
    private readonly NotificacaoQueryRepository _repo;

    public ContadorNaoLidasQueryHandlers(NotificacaoQueryRepository repo) => _repo = repo;

    public async Task<ContadorNaoLidasDto> Handle(ContadorNaoLidasQuery query)
    {
        var total = await _repo.ContarNaoLidas(query.UsuarioId);
        return new ContadorNaoLidasDto { Total = total };
    }
}
