using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.SharedKernel.Domain;

/// <summary>
/// Base para queries que precisam de contexto do usuário autenticado.
/// Herde esta classe em todas as queries que precisam de autorização ou filtragem por usuário.
/// </summary>
public abstract class PermissionContextQuery<TResult> : IQuery<TResult>
{
    /// <summary>Id do usuário logado, preenchido pelo controller antes de despachar a query.</summary>
    public string UserId { get; set; }

    /// <summary>Perfis/roles do usuário logado.</summary>
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();

    public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
