using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Auth;

namespace Imedto.Backend.Infrastructure.Auth;

/// <summary>
/// BCrypt + pepper. O pepper é aplicado via HMAC-SHA256 antes do hash bcrypt
/// pra que dump do banco não seja suficiente pra ataque offline (atacante
/// também precisaria do pepper, que vive no AWS SSM).
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private readonly BcryptOptions _options;

    public BcryptPasswordHasher(IOptions<BcryptOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.Pepper))
            throw new InvalidOperationException("Auth:Bcrypt:Pepper não configurado.");
    }

    public string Hash(string senha)
    {
        if (string.IsNullOrEmpty(senha))
            throw new ArgumentException("Senha não pode ser vazia.", nameof(senha));

        var pepperBytes = Encoding.UTF8.GetBytes(_options.Pepper);
        var senhaBytes = Encoding.UTF8.GetBytes(senha);
        using var hmac = new HMACSHA256(pepperBytes);
        var pepperedBytes = hmac.ComputeHash(senhaBytes);

        // bcrypt aceita até 72 bytes — HMAC-SHA256 gera 32, então cabe.
        // Convertemos pra base64 pra ter string ASCII compatível com bcrypt.
        var peppered = Convert.ToBase64String(pepperedBytes);
        return BCrypt.Net.BCrypt.HashPassword(peppered, _options.WorkFactor);
    }

    public bool Verificar(string senha, string hash)
    {
        if (string.IsNullOrEmpty(senha) || string.IsNullOrEmpty(hash)) return false;

        try
        {
            var pepperBytes = Encoding.UTF8.GetBytes(_options.Pepper);
            var senhaBytes = Encoding.UTF8.GetBytes(senha);
            using var hmac = new HMACSHA256(pepperBytes);
            var pepperedBytes = hmac.ComputeHash(senhaBytes);
            var peppered = Convert.ToBase64String(pepperedBytes);
            return BCrypt.Net.BCrypt.Verify(peppered, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
