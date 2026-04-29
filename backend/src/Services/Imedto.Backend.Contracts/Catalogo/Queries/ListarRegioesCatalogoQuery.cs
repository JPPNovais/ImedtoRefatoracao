using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Catalogo.Queries;

public class ListarRegioesCatalogoQuery : IQuery<IEnumerable<RegiaoCatalogoDto>>
{
    public string? Vista { get; set; }
    public bool ApenasAtivas { get; set; } = true;
}
