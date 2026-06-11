using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Queries;

/// <summary>
/// Retorna pendências abertas de um paciente no tenant para o painel persistente (CA68/CA74).
/// Singleton: usa Dapper via PendenciaQueryRepository — sem estado por request.
/// Multi-tenant: filtro por estabelecimentoId obrigatório (R5/CA69).
/// </summary>
public class ListarPendenciasAbertasQueryHandler : IRequestHandler<ListarPendenciasAbertasQuery, IReadOnlyList<PendenciaAbertaDto>>
{
    private readonly PendenciaQueryRepository _repo;

    public ListarPendenciasAbertasQueryHandler(PendenciaQueryRepository repo)
        => _repo = repo;

    public Task<IReadOnlyList<PendenciaAbertaDto>> Handle(ListarPendenciasAbertasQuery query)
        => _repo.ListarAbertas(query.PacienteId, query.EstabelecimentoId);
}
