namespace Imedto.Backend.SharedKernel.Tenancy;

/// <summary>
/// Expõe o contexto de tenant da request atual — populado pelo <c>RequiresEstabelecimentoAttribute</c>
/// depois de validar que o usuário autenticado tem acesso ao estabelecimento informado no header
/// <c>X-Estabelecimento-Id</c>. Handlers e query repositories devem filtrar tudo por
/// <see cref="EstabelecimentoId"/> para garantir isolamento multi-tenant.
/// </summary>
public interface ICurrentTenantAccessor
{
    /// <summary>Id do estabelecimento ativo na request. Lança se não foi validado.</summary>
    long EstabelecimentoId { get; }

    /// <summary>Id do usuário autenticado (sub do JWT).</summary>
    Guid UsuarioId { get; }

    /// <summary>Papel do usuário neste estabelecimento: "Dono" ou "Profissional".</summary>
    string Papel { get; }

    bool EhDono { get; }
    bool TemTenantDefinido { get; }
}
