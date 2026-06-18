using Imedto.Backend.Application.Automacoes.Commands;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Automacoes;

/// <summary>
/// Testes unitários para o helper de normalização E.164 do handler de lembretes.
/// Os testes de envio end-to-end (CA1–CA12) usam Moq sobre IEmailService/IWhatsappService.
/// </summary>
[TestFixture]
public class EnviarLembretesWhatsappNormalizacaoE164Tests
{
    [TestCase("11999998888", "+5511999998888")]   // Celular 11 dígitos
    [TestCase("1133334444",  "+551133334444")]    // Fixo 10 dígitos
    [TestCase("5511999998888", "+5511999998888")] // Já tem DDI 55 (13 dígitos)
    [TestCase("551133334444", "+551133334444")]   // Já tem DDI 55 (12 dígitos)
    public void TentarNormalizarE164_TelefoneValido_RetornaVerdadeiroENumeroFormatado(
        string telefone, string esperado)
    {
        var ok = EnviarLembretesAgendamentosCommandHandler.TentarNormalizarE164(telefone, out var resultado);

        Assert.That(ok, Is.True);
        Assert.That(resultado, Is.EqualTo(esperado));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("119999")]       // Muito curto
    [TestCase("119999988889999")] // Muito longo
    [TestCase("abc")]          // Não numérico
    public void TentarNormalizarE164_TelefoneInvalido_RetornaFalso(string? telefone)
    {
        var ok = EnviarLembretesAgendamentosCommandHandler.TentarNormalizarE164(telefone, out var resultado);

        Assert.That(ok, Is.False);
        Assert.That(resultado, Is.Null);
    }
}
