namespace Imedto.Backend.Domain.Auth;

/// <summary>
/// Repositório de códigos de recuperação 2FA (1:N com usuario_2fa).
/// Todos os métodos de leitura retornam apenas os disponíveis (não usados).
/// </summary>
public interface IUsuario2faCodigoRecuperacaoRepository
{
    /// <summary>Retorna todos os códigos de recuperação do usuário (usados e não usados).</summary>
    Task<IReadOnlyList<Usuario2faCodigoRecuperacao>> ListarPorUsuario(Guid usuarioId);

    Task Adicionar(Usuario2faCodigoRecuperacao codigo);
    void Atualizar(Usuario2faCodigoRecuperacao codigo);

    /// <summary>Remove todos os códigos do usuário (chamado na desativação do 2FA).</summary>
    Task RemoverTodosDoUsuario(Guid usuarioId);
}
