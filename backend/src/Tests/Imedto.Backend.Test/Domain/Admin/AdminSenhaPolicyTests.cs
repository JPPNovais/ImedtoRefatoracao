using Imedto.Backend.Domain.Admin;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Admin;

/// <summary>
/// Cobre a política de senha de administrador (CA7).
/// Dev: ≥ 6 chars. Prod: ≥ 10 chars + maiúscula + minúscula + número + especial.
/// </summary>
[TestFixture]
public class AdminSenhaPolicyTests
{
    // ── Modo Development ──────────────────────────────────────────────────────

    [Test]
    public void Dev_SenhaComMinimoCaracteres_NaoLanca()
    {
        Assert.DoesNotThrow(() => AdminSenhaPolicy.Validar("123456", isDevelopment: true));
    }

    [Test]
    public void Dev_SenhaMuitoCurta_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            AdminSenhaPolicy.Validar("12345", isDevelopment: true));
        Assert.That(ex!.Message, Does.Contain("6"));
    }

    [Test]
    public void Dev_SenhaVazia_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            AdminSenhaPolicy.Validar(string.Empty, isDevelopment: true));
        Assert.That(ex!.Message, Is.Not.Empty);
    }

    // ── Modo Produção ─────────────────────────────────────────────────────────

    [Test]
    public void Prod_SenhaCompleta_NaoLanca()
    {
        Assert.DoesNotThrow(() =>
            AdminSenhaPolicy.Validar("Senha@123!", isDevelopment: false));
    }

    [Test]
    public void Prod_SenhaSemMaiuscula_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            AdminSenhaPolicy.Validar("senha@123!", isDevelopment: false));
        Assert.That(ex!.Message, Does.Contain("maiúscula"));
    }

    [Test]
    public void Prod_SenhaSemNumero_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            AdminSenhaPolicy.Validar("SenhaForte!abc", isDevelopment: false));
        Assert.That(ex!.Message, Does.Contain("número"));
    }

    [Test]
    public void Prod_SenhaSemEspecial_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            AdminSenhaPolicy.Validar("SenhaForte123", isDevelopment: false));
        Assert.That(ex!.Message, Does.Contain("especial"));
    }

    [Test]
    public void Prod_SenhaMenosDe10Chars_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            AdminSenhaPolicy.Validar("A1!", isDevelopment: false));
        Assert.That(ex!.Message, Does.Contain("10"));
    }

    // ── Geração de senha temporária ───────────────────────────────────────────

    [Test]
    public void GerarSenhaTemporaria_TamanhoEPolitica()
    {
        for (var i = 0; i < 20; i++)
        {
            var senha = AdminSenhaPolicy.GerarSenhaTemporaria();
            Assert.That(senha.Length, Is.EqualTo(20));
            // A senha gerada deve passar pela política de produção.
            Assert.DoesNotThrow(() => AdminSenhaPolicy.Validar(senha, isDevelopment: false),
                $"Senha gerada '{senha}' não passou pela política de produção.");
        }
    }
}
