using Imedto.Backend.Contracts.Auth.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Auth.Queries;

/// <summary>
/// Reidrata todo o estado de auth da SPA em um único round-trip:
/// usuário, perfil profissional (se houver) e estabelecimentos vinculados.
/// </summary>
public class BootstrapMeQuery : IQuery<BootstrapMeDto>
{
    public Guid UsuarioId { get; set; }
}
