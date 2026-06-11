using Imedto.Backend.Contracts.Convenios.Queries;
using Imedto.Backend.Contracts.Convenios.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Convenios.Queries;

/// <summary>
/// Lista convênios do estabelecimento. Singleton (Dapper, sem estado por request).
/// </summary>
public class ListarConveniosQueryHandler : IRequestHandler<ListarConveniosQuery, IReadOnlyList<ConvenioListadoDto>>
{
    private readonly ConvenioQueryRepository _repo;
    public ListarConveniosQueryHandler(ConvenioQueryRepository repo) => _repo = repo;

    public Task<IReadOnlyList<ConvenioListadoDto>> Handle(ListarConveniosQuery query)
        => _repo.ListarConvenios(query.EstabelecimentoId, query.ApenasAtivos);
}

/// <summary>
/// Obtém detalhe de um convênio (com planos). Singleton.
/// </summary>
public class ObterConvenioQueryHandler : IRequestHandler<ObterConvenioQuery, ConvenioDetalheDto?>
{
    private readonly ConvenioQueryRepository _repo;
    public ObterConvenioQueryHandler(ConvenioQueryRepository repo) => _repo = repo;

    public Task<ConvenioDetalheDto?> Handle(ObterConvenioQuery query)
        => _repo.ObterDetalhe(query.ConvenioId, query.EstabelecimentoId);
}
