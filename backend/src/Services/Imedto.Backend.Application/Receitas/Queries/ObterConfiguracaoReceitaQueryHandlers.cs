using Imedto.Backend.Contracts.Receitas.Queries;
using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Receitas.Queries;

/// <summary>
/// Configuração de receita do estabelecimento. Se não existir, devolve um DTO
/// "vazio" (com apenas o id) — UI mostra valores em branco para o dono editar.
/// </summary>
public class ObterConfiguracaoReceitaQueryHandlers
    : IRequestHandler<ObterConfiguracaoReceitaQuery, ConfiguracaoReceitaDto>
{
    private readonly IReceitaQueryRepository _queryRepo;

    public ObterConfiguracaoReceitaQueryHandlers(IReceitaQueryRepository queryRepo)
    {
        _queryRepo = queryRepo;
    }

    public async Task<ConfiguracaoReceitaDto> Handle(ObterConfiguracaoReceitaQuery query)
    {
        var dto = await _queryRepo.ObterConfiguracao(query.EstabelecimentoId);
        return dto ?? new ConfiguracaoReceitaDto { EstabelecimentoId = query.EstabelecimentoId };
    }
}
