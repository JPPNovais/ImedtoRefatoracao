using Imedto.Backend.Contracts.ModelosPermissao.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.ModelosPermissao.Queries;

public class ListarModelosPermissaoQuery : IQuery<IEnumerable<ModeloPermissaoDto>>
{
    public long EstabelecimentoId { get; set; }
}
