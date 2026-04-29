using Imedto.Backend.Contracts.Relatorios.Queries;
using Imedto.Backend.Contracts.Relatorios.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Relatorios.Queries;

/// <summary>
/// Handler do relatório de orçamentos.
/// </summary>
public class RelatorioOrcamentosQueryHandler : IRequestHandler<RelatorioOrcamentosQuery, RelatorioOrcamentosDto>
{
    private readonly RelatorioQueryRepository _repo;

    public RelatorioOrcamentosQueryHandler(RelatorioQueryRepository repo) => _repo = repo;

    public Task<RelatorioOrcamentosDto> Handle(RelatorioOrcamentosQuery query)
    {
        FiltrosRelatorio.Validar(query.DataInicio, query.DataFim);
        return _repo.RelatorioOrcamentos(query.EstabelecimentoId, query.DataInicio, query.DataFim);
    }
}
