using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Auth;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Admin;

/// <summary>
/// Cobre o emissor de JWT admin:
/// - Claim imedto_admin = "true" presente.
/// - Claim estabelecimento_id NUNCA presente.
/// - must_reset_password injetado quando forcePasswordReset = true.
/// - Refresh token tem hash diferente do token cru.
/// </summary>
[TestFixture]
public class ImedtoAdminTokenIssuerTests
{
    private ImedtoAdminTokenIssuer _issuer = null!;

    // Chave ECDSA P-256 gerada com openssl para uso exclusivo em testes.
    private const string TestPrivateKeyPem = """
        -----BEGIN PRIVATE KEY-----
        MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQg4rVhbCXKM+68CDIH
        LnPByf2Mh9DSwshlql9sTciawGKhRANCAARfibrvziUu4MYmCjD+a8slWe2e6bQO
        NdAXlzYAoIP93/B+BmfjGg0K5FeJd/VdFA8SeYfHlPPa2l4EPtDnvcs4
        -----END PRIVATE KEY-----
        """;

    [SetUp]
    public void Setup()
    {
        var options = Options.Create(new JwtAuthOptions
        {
            Issuer = "imedto-backend",
            Audience = "imedto-app",
            PrivateKeyPem = TestPrivateKeyPem,
            PublicKeyPem = string.Empty, // não usado na emissão
            AccessTokenLifetime = TimeSpan.FromMinutes(15),
            RefreshTokenLifetime = TimeSpan.FromDays(30)
        });

        var mockIssuer = new Mock<Imedto.Backend.Domain.Auth.IJwtTokenIssuer>();
        _issuer = new ImedtoAdminTokenIssuer(
            mockIssuer.Object,
            options,
            NullLogger<ImedtoAdminTokenIssuer>.Instance);
    }

    [Test]
    public void EmitirAccessToken_ContendoClaimAdminTrue()
    {
        var admin = ImedtoAdmin.Criar("admin@imedto.com", "Admin", "hash", false, null);
        var token = _issuer.EmitirAccessToken(admin, forcePasswordReset: false);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.Token);

        Assert.That(jwt.Claims.Any(c => c.Type == "imedto_admin" && c.Value == "true"),
            "Claim imedto_admin = true deve estar presente.");
    }

    [Test]
    public void EmitirAccessToken_NaoContemEstabelecimentoId()
    {
        var admin = ImedtoAdmin.Criar("admin@imedto.com", "Admin", "hash", false, null);
        var token = _issuer.EmitirAccessToken(admin, forcePasswordReset: false);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.Token);

        Assert.That(jwt.Claims.Any(c => c.Type == "estabelecimento_id"),
            Is.False, "Claim estabelecimento_id NÃO deve existir no JWT admin.");
    }

    [Test]
    public void EmitirAccessToken_ForcePasswordReset_True_InjetaClaimMustReset()
    {
        var admin = ImedtoAdmin.Criar("admin@imedto.com", "Admin", "hash", true, null);
        var token = _issuer.EmitirAccessToken(admin, forcePasswordReset: true);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.Token);

        Assert.That(jwt.Claims.Any(c => c.Type == "must_reset_password" && c.Value == "true"),
            "Claim must_reset_password = true deve estar presente quando forcePasswordReset.");
    }

    [Test]
    public void EmitirAccessToken_ForcePasswordReset_False_NaoInjetaClaimMustReset()
    {
        var admin = ImedtoAdmin.Criar("admin@imedto.com", "Admin", "hash", false, null);
        var token = _issuer.EmitirAccessToken(admin, forcePasswordReset: false);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.Token);

        Assert.That(jwt.Claims.Any(c => c.Type == "must_reset_password"),
            Is.False, "Claim must_reset_password NÃO deve existir quando não forçado.");
    }

    [Test]
    public void GerarRefreshToken_HashDiferenteDoCru()
    {
        var (cru, hash, expira) = ImedtoAdminTokenIssuer.GerarRefreshToken();

        Assert.That(string.IsNullOrEmpty(cru), Is.False);
        Assert.That(string.IsNullOrEmpty(hash), Is.False);
        Assert.That(cru, Is.Not.EqualTo(hash));
        Assert.That(expira, Is.GreaterThan(DateTimeOffset.UtcNow));
    }

    [Test]
    public void HashToken_Determinista()
    {
        var hash1 = ImedtoAdminTokenIssuer.HashToken("meu-token");
        var hash2 = ImedtoAdminTokenIssuer.HashToken("meu-token");

        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void EmitirAccessToken_SubEEmailCorretos()
    {
        var admin = ImedtoAdmin.Criar("test@imedto.com", "Test Admin", "hash", false, null);
        var token = _issuer.EmitirAccessToken(admin, forcePasswordReset: false);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.Token);

        Assert.That(jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value,
            Is.EqualTo(admin.Id.ToString()));
        Assert.That(jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
            Is.EqualTo("test@imedto.com"));
    }
}
