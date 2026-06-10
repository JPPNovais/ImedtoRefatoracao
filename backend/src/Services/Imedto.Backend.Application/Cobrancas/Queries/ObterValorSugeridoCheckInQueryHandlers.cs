using Imedto.Backend.Contracts.Cobrancas.Queries;
using Imedto.Backend.Contracts.Cobrancas.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories.Cobrancas;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Cobrancas.Queries;

public class ObterValorSugeridoCheckInQueryHandlers : IRequestHandler<ObterValorSugeridoCheckInQuery, ValorSugeridoCheckInDto>
{
    private readonly CobrancaQueryRepository _repo;

    public ObterValorSugeridoCheckInQueryHandlers(CobrancaQueryRepository repo)
        => _repo = repo;

    public async Task<ValorSugeridoCheckInDto> Handle(ObterValorSugeridoCheckInQuery query)
    {
        var valor = await _repo.ObterValorSugerido(query.EstabelecimentoId, query.ProfissionalUsuarioId);
        return new ValorSugeridoCheckInDto { ValorSugerido = valor };
    }
}
