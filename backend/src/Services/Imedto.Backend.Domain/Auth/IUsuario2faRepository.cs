namespace Imedto.Backend.Domain.Auth;

/// <summary>
/// Repositório de estado 2FA do usuário.
/// A entidade <see cref="Usuario2fa"/> é 1:1 com <c>usuarios.id</c>; PK = usuario_id.
/// </summary>
public interface IUsuario2faRepository
{
    /// <summary>Retorna o estado 2FA do usuário (null se nenhum registro criado).</summary>
    Task<Usuario2fa> ObterPorUsuarioId(Guid usuarioId);

    Task Adicionar(Usuario2fa estado);
    void Atualizar(Usuario2fa estado);

    /// <summary>Remove o registro de 2FA e todos os códigos de recuperação.</summary>
    void Remover(Usuario2fa estado);
}
