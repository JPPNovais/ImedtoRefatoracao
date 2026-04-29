using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Catalogo.Queries;

public class ListarEspecialidadesQueryHandlers : IRequestHandler<ListarEspecialidadesQuery, IEnumerable<EspecialidadeListadaDto>>
{
    private readonly CatalogoQueryRepository _repo;

    public ListarEspecialidadesQueryHandlers(CatalogoQueryRepository repo) => _repo = repo;

    public Task<IEnumerable<EspecialidadeListadaDto>> Handle(ListarEspecialidadesQuery query)
        => _repo.ListarEspecialidades(query.ProfissaoId, query.ApenasAtivas);
}
