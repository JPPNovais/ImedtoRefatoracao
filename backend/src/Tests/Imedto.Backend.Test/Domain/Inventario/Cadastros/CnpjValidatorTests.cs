using Imedto.Backend.Domain.Inventario.Cadastros;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Inventario.Cadastros;

/// <summary>
/// Testes do <see cref="CnpjValidator"/> com suporte a CNPJ alfanumérico (IN RFB 2.229/2024).
/// Vetores canônicos validados pelo algoritmo oficial (ASCII − 48).
/// </summary>
[TestFixture]
public class CnpjValidatorTests
{
    // ─── CA1 — retrocompatibilidade numérica ────────────────────────────────

    [Test]
    [TestCase("11222333000181")]           // canônico sem máscara
    [TestCase("11.222.333/0001-81")]       // com máscara
    [TestCase("12345678000195")]           // outro CNPJ numérico válido
    [TestCase("12.345.678/0001-95")]       // com máscara
    public void EhValido_CnpjNumericoValido_RetornaTrue(string cnpj)
    {
        Assert.That(CnpjValidator.EhValido(cnpj), Is.True, $"CNPJ numérico válido rejeitado: {cnpj}");
    }

    [Test]
    [TestCase("11111111111111")]           // todos iguais
    [TestCase("00000000000000")]           // todos zeros
    [TestCase("12345678000100")]           // DV errado
    [TestCase("1234")]                     // muito curto
    [TestCase("")]
    [TestCase(null)]
    public void EhValido_CnpjNumericoInvalido_RetornaFalse(string? cnpj)
    {
        Assert.That(CnpjValidator.EhValido(cnpj), Is.False, $"CNPJ numérico inválido deveria ser rejeitado: {cnpj}");
    }

    // ─── CA2 — caminho feliz alfanumérico ───────────────────────────────────

    [Test]
    [TestCase("12ABC34501DE35")]           // canônico sem máscara
    [TestCase("12.ABC.345/01DE-35")]       // com máscara
    [TestCase("12.abc.345/01de-35")]       // minúscula — normalizado para upper
    public void EhValido_CnpjAlfanumericoValido_RetornaTrue(string cnpj)
    {
        Assert.That(CnpjValidator.EhValido(cnpj), Is.True, $"CNPJ alfanumérico válido rejeitado: {cnpj}");
    }

    // ─── CA3 — DV errado rejeitado ──────────────────────────────────────────

    [Test]
    [TestCase("12ABC34501DE34")]           // DV correto seria 35
    [TestCase("12.ABC.345/01DE-34")]       // com máscara
    public void EhValido_CnpjAlfanumericoDvErrado_RetornaFalse(string cnpj)
    {
        Assert.That(CnpjValidator.EhValido(cnpj), Is.False, $"CNPJ com DV errado deveria ser rejeitado: {cnpj}");
    }

    // ─── CA5 — caractere fora do alfabeto válido rejeitado ──────────────────

    [Test]
    [TestCase("12@BC34501DE35")]           // @ inválido
    [TestCase("12.ÇBC.345/01DE-35")]      // cedilha inválida
    public void EhValido_CaractereInvalido_RetornaFalse(string cnpj)
    {
        Assert.That(CnpjValidator.EhValido(cnpj), Is.False, $"CNPJ com caractere inválido deveria ser rejeitado: {cnpj}");
    }

    // ─── CA6 — DV com letra rejeitado ───────────────────────────────────────

    [Test]
    [TestCase("12ABC34501DE3A")]           // posição 13 com letra
    [TestCase("12ABC34501DEAA")]           // posições 13 e 14 com letra
    public void EhValido_DvComLetra_RetornaFalse(string cnpj)
    {
        Assert.That(CnpjValidator.EhValido(cnpj), Is.False, $"DV com letra deveria ser rejeitado: {cnpj}");
    }

    // ─── Normalizar ─────────────────────────────────────────────────────────

    [Test]
    [TestCase("12.ABC.345/01DE-35", "12ABC34501DE35")]
    [TestCase("12abc34501de35",     "12ABC34501DE35")]  // uppercase ao normalizar
    [TestCase("11.222.333/0001-81", "11222333000181")]  // numérico retrocompat
    public void Normalizar_FormatarParaFormaCanonicaUppercase(string entrada, string esperado)
    {
        Assert.That(CnpjValidator.Normalizar(entrada), Is.EqualTo(esperado));
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Normalizar_EntradaVaziaOuNula_RetornaNulo(string? entrada)
    {
        Assert.That(CnpjValidator.Normalizar(entrada), Is.Null);
    }
}
