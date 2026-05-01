using Imedto.Backend.Contracts.Agendamentos.Queries;
using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Agendamentos.Queries;

public class ListarListaEsperaQueryHandler : IRequestHandler<ListarListaEsperaQuery, IEnumerable<ListaEsperaItemDto>>
{
    private readonly ListaEsperaQueryRepository _repo;
    public ListarListaEsperaQueryHandler(ListaEsperaQueryRepository repo) => _repo = repo;
    public Task<IEnumerable<ListaEsperaItemDto>> Handle(ListarListaEsperaQuery q)
        => _repo.Listar(q.EstabelecimentoId);
}
