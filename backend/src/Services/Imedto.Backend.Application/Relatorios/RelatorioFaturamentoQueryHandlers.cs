using Imedto.Backend.Contracts.Relatorios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Relatorios;

public class RelatorioFaturamentoQueryHandlers : IRequestHandler<RelatorioFaturamentoQuery, IEnumerable<FaturamentoCategoriaDto>>
{
    private readonly RelatorioQueryRepository _repo;

    public RelatorioFaturamentoQueryHandlers(RelatorioQueryRepository repo) => _repo = repo;

    public Task<IEnumerable<FaturamentoCategoriaDto>> Handle(RelatorioFaturamentoQuery query)
        => _repo.RelatorioFaturamento(query.EstabelecimentoId, query.DataInicio, query.DataFim);
}
