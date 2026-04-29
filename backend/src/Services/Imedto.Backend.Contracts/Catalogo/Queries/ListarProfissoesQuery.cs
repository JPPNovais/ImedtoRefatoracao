using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Catalogo.Queries;

public class ListarProfissoesQuery : IQuery<IEnumerable<ProfissaoListadaDto>>
{
    public bool ApenasAtivas { get; set; } = true;
}
