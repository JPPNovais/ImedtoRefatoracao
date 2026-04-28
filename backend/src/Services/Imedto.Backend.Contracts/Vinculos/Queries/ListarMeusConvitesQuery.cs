using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Queries;

public class ListarMeusConvitesQuery : IQuery<IEnumerable<ConviteDto>>
{
    public Guid UsuarioId { get; set; }
}
