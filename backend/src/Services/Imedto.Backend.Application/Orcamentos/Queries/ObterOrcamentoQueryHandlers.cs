using Imedto.Backend.Contracts.Orcamentos.Queries;
using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Queries;

public class ObterOrcamentoQueryHandlers : IRequestHandler<ObterOrcamentoQuery, OrcamentoDto>
{
    private readonly OrcamentoQueryRepository _repo;

    public ObterOrcamentoQueryHandlers(OrcamentoQueryRepository repo) => _repo = repo;

    public async Task<OrcamentoDto> Handle(ObterOrcamentoQuery query)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var dto = await _repo.ObterPorId(query.OrcamentoId, query.EstabelecimentoId)
            ?? throw new BusinessException("Orçamento não encontrado.");
        return dto;
    }
}
