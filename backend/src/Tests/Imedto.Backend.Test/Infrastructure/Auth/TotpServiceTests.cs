using Imedto.Backend.Infrastructure.Auth;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Auth;

/// <summary>
/// Testes unitários do algoritmo TOTP RFC 6238 (CA3/CA6).
///
/// RFC test vectors de https://datatracker.ietf.org/doc/html/rfc6238#appendix-B
/// são para SHA256/SHA512 — nossa implementação é SHA1.
/// Validamos aqui: geração/validação com janela ±1 step, base32, URI.
/// </summary>
[TestFixture]
public class TotpServiceTests
{
    // ── Geração determinística ──────────────────────────────────────────────

    [Test]
    public void GerarCodigo_MesmoSegredoEMomentoProduzMesmoCodigo()
    {
        var segredo = TotpService.GerarSegredoBase32();
        var momento = DateTime.UtcNow;

        var c1 = TotpService.GerarCodigo(segredo, momento);
        var c2 = TotpService.GerarCodigo(segredo, momento);

        Assert.That(c1, Is.EqualTo(c2));
        Assert.That(c1, Has.Length.EqualTo(6));
        Assert.That(c1, Does.Match(@"^\d{6}$"));
    }

    // ── Validação — janela ±1 step ──────────────────────────────────────────

    [Test]
    public void Validar_CodigoAtual_Aceito()
    {
        var segredo = TotpService.GerarSegredoBase32();
        var agora = DateTime.UtcNow;
        var codigo = TotpService.GerarCodigo(segredo, agora);

        Assert.That(TotpService.Validar(segredo, codigo, agora), Is.True);
    }

    [Test]
    public void Validar_CodigoStepAnterior_DentroJanela_Aceito()
    {
        var segredo = TotpService.GerarSegredoBase32();
        var agora = DateTime.UtcNow;
        var momentoAnterior = agora.AddSeconds(-30);
        var codigoAnterior = TotpService.GerarCodigo(segredo, momentoAnterior);

        // Valida no momento atual com código do step anterior
        Assert.That(TotpService.Validar(segredo, codigoAnterior, agora), Is.True);
    }

    [Test]
    public void Validar_CodigoStepSeguinte_DentroJanela_Aceito()
    {
        var segredo = TotpService.GerarSegredoBase32();
        var agora = DateTime.UtcNow;
        var momentoFuturo = agora.AddSeconds(30);
        var codigoFuturo = TotpService.GerarCodigo(segredo, momentoFuturo);

        Assert.That(TotpService.Validar(segredo, codigoFuturo, agora), Is.True);
    }

    [Test]
    public void Validar_CodigoMuitoAntigo_Rejeitado()
    {
        var segredo = TotpService.GerarSegredoBase32();
        var agora = DateTime.UtcNow;
        var muitoAntigo = agora.AddSeconds(-90);
        var codigoVelho = TotpService.GerarCodigo(segredo, muitoAntigo);

        Assert.That(TotpService.Validar(segredo, codigoVelho, agora), Is.False);
    }

    [Test]
    public void Validar_CodigoErrado_Rejeitado()
    {
        var segredo = TotpService.GerarSegredoBase32();
        Assert.That(TotpService.Validar(segredo, "000000"), Is.False);
    }

    [Test]
    public void Validar_CodigoNaoNumerico_Rejeitado()
    {
        var segredo = TotpService.GerarSegredoBase32();
        Assert.That(TotpService.Validar(segredo, "ABCDEF"), Is.False);
    }

    [Test]
    public void Validar_CodigoTamanhoErrado_Rejeitado()
    {
        var segredo = TotpService.GerarSegredoBase32();
        Assert.That(TotpService.Validar(segredo, "12345"), Is.False);
        Assert.That(TotpService.Validar(segredo, "1234567"), Is.False);
    }

    // ── Segredo gerado com entropia suficiente ──────────────────────────────

    [Test]
    public void GerarSegredoBase32_Comprimento_PeloMenos32Caracteres()
    {
        // 20 bytes × 8/5 bits = 32 chars base32
        var segredo = TotpService.GerarSegredoBase32();
        Assert.That(segredo, Has.Length.EqualTo(32));
        Assert.That(segredo, Does.Match(@"^[A-Z2-7]+$"));
    }

    [Test]
    public void GerarSegredoBase32_ChavadasUnicas()
    {
        var s1 = TotpService.GerarSegredoBase32();
        var s2 = TotpService.GerarSegredoBase32();
        Assert.That(s1, Is.Not.EqualTo(s2));
    }

    // ── URI otpauth:// ──────────────────────────────────────────────────────

    [Test]
    public void MontarOtpauthUri_FormatoCorreto()
    {
        var segredo = "JBSWY3DPEHPK3PXP";
        var uri = TotpService.MontarOtpauthUri("usuario@imedto.com", segredo);

        // Label é URL-encoded conforme RFC 4516 / Google Key URI Format:
        // "Imedto:usuario@imedto.com" → "Imedto%3Ausuario%40imedto.com"
        Assert.That(uri, Does.StartWith("otpauth://totp/Imedto%3A"));
        Assert.That(uri, Does.Contain($"secret={segredo}"));
        Assert.That(uri, Does.Contain("issuer=Imedto"));
        Assert.That(uri, Does.Contain("algorithm=SHA1"));
        Assert.That(uri, Does.Contain("digits=6"));
        Assert.That(uri, Does.Contain("period=30"));
    }

    // ── Base32 roundtrip ────────────────────────────────────────────────────

    [Test]
    public void Base32_EncodeDecodeRoundtrip()
    {
        var original = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };
        var encoded = TotpService.EncodeBase32(original);
        var decoded = TotpService.DecodeBase32(encoded);

        Assert.That(decoded.Take(original.Length).ToArray(), Is.EqualTo(original));
    }
}
