using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Financeiro.Queries;

public class ObterConfigComissaoQueryHandler : IRequestHandler<ObterConfigComissaoQuery, ConfigComissaoDto>
{
    private readonly ConsolidacaoFinanceiraQueryRepository _repo;

    public ObterConfigComissaoQueryHandler(ConsolidacaoFinanceiraQueryRepository repo) => _repo = repo;

    public async Task<ConfigComissaoDto> Handle(ObterConfigComissaoQuery query)
    {
        var (consulta, procedimento) = await _repo.ObterConfigComissao(
            query.EstabelecimentoId, query.ProfissionalUsuarioId);

        return new ConfigComissaoDto
        {
            PercentualConsulta = consulta,
            PercentualProcedimento = procedimento,
            PercentualPadrao = ComissaoConfig.PercentualPadrao
        };
    }
}
