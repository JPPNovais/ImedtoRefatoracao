using Imedto.Backend.Contracts.Orcamentos.Queries;
using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Queries;

public class ObterOrcamentoCompletoQueryHandlers : IRequestHandler<ObterOrcamentoCompletoQuery, OrcamentoCompletoDto>
{
    private readonly OrcamentoQueryRepository _repo;

    public ObterOrcamentoCompletoQueryHandlers(OrcamentoQueryRepository repo) => _repo = repo;

    public async Task<OrcamentoCompletoDto> Handle(ObterOrcamentoCompletoQuery query)
    {
        var dto = await _repo.ObterCompletoPorId(query.OrcamentoId);
        if (dto is null || dto.EstabelecimentoId != query.EstabelecimentoId)
            throw new BusinessException("Orçamento não encontrado.");
        return dto;
    }
}
