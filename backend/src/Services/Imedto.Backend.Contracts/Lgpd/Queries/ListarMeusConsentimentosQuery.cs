using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Lgpd.Queries;

public class ListarMeusConsentimentosQuery : IQuery<IEnumerable<ConsentimentoDto>>
{
    public Guid UsuarioId { get; init; }
}
