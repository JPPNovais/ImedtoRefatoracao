using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Queries;

public class ObterConfigComissaoQuery : IQuery<ConfigComissaoDto>
{
    public long EstabelecimentoId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
}
