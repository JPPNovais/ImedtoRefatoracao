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
        var dto = await _repo.ObterPorId(query.OrcamentoId);
        if (dto is null || dto.EstabelecimentoId != query.EstabelecimentoId)
            throw new BusinessException("Orçamento não encontrado.");
        return dto;
    }
}
