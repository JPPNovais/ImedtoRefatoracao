using Imedto.Backend.Contracts.Salas.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Salas.Queries;

public class ListarTiposSalaQuery : IQuery<IEnumerable<TipoSalaDto>>
{
}
