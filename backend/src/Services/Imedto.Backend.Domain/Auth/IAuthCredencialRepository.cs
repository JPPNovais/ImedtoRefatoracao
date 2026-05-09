namespace Imedto.Backend.Domain.Auth;

public interface IAuthCredencialRepository
{
    Task<AuthCredencial> ObterPorEmailAsync(string email);
    Task<AuthCredencial> ObterPorIdAsync(Guid usuarioId);
    Task<bool> ExisteParaEmailAsync(string email);
    Task AdicionarAsync(AuthCredencial credencial);
    void Atualizar(AuthCredencial credencial);
    void Remover(AuthCredencial credencial);
}

public interface IAuthRefreshTokenRepository
{
    Task<AuthRefreshToken> ObterPorHashAsync(string tokenHash);
    Task AdicionarAsync(AuthRefreshToken token);
    void Atualizar(AuthRefreshToken token);
    Task RevogarTodosDoUsuarioAsync(Guid usuarioId);
}

public interface IAuthEmailTokenRepository
{
    Task<AuthEmailToken> ObterValidoPorHashAsync(string tokenHash, AuthEmailTokenTipo tipo);

    /// <summary>
    /// Retorna o último token criado (consumido ou não) deste tipo para o usuário.
    /// Usado para implementar cooldown de reenvio (anti-spam).
    /// </summary>
    Task<AuthEmailToken> ObterUltimoCriadoAsync(Guid usuarioId, AuthEmailTokenTipo tipo);

    Task AdicionarAsync(AuthEmailToken token);
    void Atualizar(AuthEmailToken token);
}
