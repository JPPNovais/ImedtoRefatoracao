using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Test.Helpers;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Pacientes;

/// <summary>
/// Unit tests do CpfValidator — algoritmo de digito verificador da Receita.
/// Cobertura: CPFs validos formatados/sem formatar, DV invalido, sequencias
/// repetidas, comprimento incorreto, null/empty.
/// </summary>
[TestFixture]
public class CpfValidatorTests
{
    [TestCase("123.456.789-09")]
    [TestCase("12345678909")]
    [TestCase("111.444.777-35")]
    [TestCase("11144477735")]
    public void EhValido_CpfComDvCorreto_RetornaTrue(string cpf)
    {
        Assert.That(CpfValidator.EhValido(cpf), Is.True);
    }

    [TestCase("123.456.789-00")] // DV1 errado
    [TestCase("12345678900")]    // DV2 errado
    [TestCase("11144477734")]    // DV2 off-by-one
    public void EhValido_CpfComDvIncorreto_RetornaFalse(string cpf)
    {
        Assert.That(CpfValidator.EhValido(cpf), Is.False);
    }

    [TestCase("000.000.000-00")]
    [TestCase("11111111111")]
    [TestCase("22222222222")]
    [TestCase("99999999999")]
    public void EhValido_SequenciasRepetidas_RetornaFalse(string cpf)
    {
        // Sequencias repetidas (00...0, 11...1, ..., 99...9) sempre passariam
        // no algoritmo de DV — bug classico. Devem ser explicitamente rejeitadas.
        Assert.That(CpfValidator.EhValido(cpf), Is.False);
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("   ")]
    [TestCase("123")]
    [TestCase("123.456.789-099")] // 12 digitos
    [TestCase("abc.def.ghi-jk")]  // sem digito
    public void EhValido_StringInvalida_RetornaFalse(string cpf)
    {
        Assert.That(CpfValidator.EhValido(cpf), Is.False);
    }

    [Test]
    public void EhValido_TodosOsCpfsDoCpfTestData_RetornamTrue()
    {
        // Garante que o helper de testes esta calibrado com o validator de producao.
        foreach (var cpf in CpfTestData.Validos)
            Assert.That(CpfValidator.EhValido(cpf), Is.True, $"CPF inesperadamente invalido: {cpf}");
    }
}
