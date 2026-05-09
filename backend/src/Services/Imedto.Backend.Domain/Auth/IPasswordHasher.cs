namespace Imedto.Backend.Domain.Auth;

/// <summary>
/// Abstração de hashing de senha. Implementação atual usa BCrypt + pepper do AWS SSM.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Gera hash seguro da senha (incorpora salt aleatório e pepper).</summary>
    string Hash(string senha);

    /// <summary>Verifica se a senha bate com o hash. Constante em tempo (resistente a timing attacks).</summary>
    bool Verificar(string senha, string hash);
}
