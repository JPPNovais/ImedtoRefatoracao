using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Queries;

public class ObterOrcamentoQuery : IQuery<OrcamentoDto>
{
    public long OrcamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
}
