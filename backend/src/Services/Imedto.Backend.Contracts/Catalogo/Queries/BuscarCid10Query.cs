using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Catalogo.Queries;

public class BuscarCid10Query : IQuery<IEnumerable<Cid10Dto>>
{
    public string? Busca { get; set; }
    public int Limite { get; set; } = 20;
}
