using Imedto.Backend.Contracts.Orcamentos.Queries;
using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Orcamentos.Queries;

public class ListarOrcamentosQueryHandlers : IRequestHandler<ListarOrcamentosQuery, IEnumerable<OrcamentoResumoDto>>
{
    private readonly OrcamentoQueryRepository _repo;

    public ListarOrcamentosQueryHandlers(OrcamentoQueryRepository repo) => _repo = repo;

    public Task<IEnumerable<OrcamentoResumoDto>> Handle(ListarOrcamentosQuery query)
        => _repo.Listar(query.EstabelecimentoId, query.PacienteId, query.Status);
}
