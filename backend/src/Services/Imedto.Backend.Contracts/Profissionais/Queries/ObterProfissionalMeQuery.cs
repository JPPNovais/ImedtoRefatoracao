using Imedto.Backend.Contracts.Profissionais.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Profissionais.Queries;

/// <summary>
/// Retorna o cadastro profissional do usuário autenticado, ou <c>null</c> se ainda
/// não se cadastrou como profissional.
/// </summary>
public class ObterProfissionalMeQuery : IQuery<ProfissionalDto>
{
    public Guid UsuarioId { get; set; }
}
