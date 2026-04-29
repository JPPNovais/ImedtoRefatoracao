using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Receitas.Queries;

public class ObterReceitaQuery : IQuery<ReceitaDto>
{
    public long ReceitaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}
