using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Receitas.Queries;

public class ObterConfiguracaoReceitaQuery : IQuery<ConfiguracaoReceitaDto>
{
    public long EstabelecimentoId { get; set; }
}
