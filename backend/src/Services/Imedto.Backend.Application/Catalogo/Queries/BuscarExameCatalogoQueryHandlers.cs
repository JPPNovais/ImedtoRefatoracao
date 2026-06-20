using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Catalogo.Queries;

public class BuscarExameCatalogoQueryHandlers : IRequestHandler<BuscarExameCatalogoQuery, IEnumerable<ExameCatalogoDto>>
{
    private readonly ExameCatalogoQueryRepository _repo;

    public BuscarExameCatalogoQueryHandlers(ExameCatalogoQueryRepository repo) => _repo = repo;

    public Task<IEnumerable<ExameCatalogoDto>> Handle(BuscarExameCatalogoQuery query)
        => _repo.Buscar(query.Busca, Math.Clamp(query.Limite, 1, 50));
}
