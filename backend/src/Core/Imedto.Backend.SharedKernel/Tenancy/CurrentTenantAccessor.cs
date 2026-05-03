namespace Imedto.Backend.SharedKernel.Tenancy;

/// <summary>
/// Implementação scoped de <see cref="ICurrentTenantAccessor"/> — mutável dentro da request
/// apenas pelo filter de tenancy; handlers tratam como somente-leitura.
/// </summary>
public class CurrentTenantAccessor : ICurrentTenantAccessor
{
    private long? _estabelecimentoId;
    private Guid _usuarioId;
    private string _papel;

    public long EstabelecimentoId => _estabelecimentoId
        ?? throw new InvalidOperationException("Tenant não definido — use [RequiresEstabelecimento] na action.");

    public Guid UsuarioId => _usuarioId;
    public string Papel => _papel;

    public bool EhDono => _papel == "Dono";
    public bool TemTenantDefinido => _estabelecimentoId.HasValue;

    /// <summary>
    /// Setado pelo <c>CurrentUserMiddleware</c> em TODA request autenticada.
    /// Permite handlers usar _tenant.UsuarioId mesmo em endpoints sem
    /// [RequiresEstabelecimento] (ex: /api/usuario/me).
    /// </summary>
    public void DefinirUsuario(Guid usuarioId)
    {
        _usuarioId = usuarioId;
    }

    /// <summary>Chamado exclusivamente pelo <c>RequiresEstabelecimentoAttribute</c>.</summary>
    public void Definir(long estabelecimentoId, Guid usuarioId, string papel)
    {
        _estabelecimentoId = estabelecimentoId;
        _usuarioId = usuarioId;
        _papel = papel;
    }
}
