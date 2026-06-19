using Imedto.Backend.Infrastructure.Ia;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Ia;

/// <summary>
/// Testes do PiiSanitizer — garante que CPF, CNPJ, e-mail, telefone, CEP e RG
/// são removidos antes de qualquer envio para IA externa (requisito LGPD).
/// </summary>
[TestFixture]
public class PiiSanitizerTests
{
    // ---- String vazia / null ----

    [Test]
    public void Sanitize_StringVazia_RetornaStringVazia()
    {
        // Arrange + Act
        var resultado = PiiSanitizer.Sanitize(string.Empty);

        // Assert
        Assert.That(resultado, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Sanitize_StringNull_RetornaNull()
    {
        // Arrange + Act
        var resultado = PiiSanitizer.Sanitize(null);

        // Assert
        Assert.That(resultado, Is.Null);
    }

    // ---- CPF ----

    [Test]
    public void Sanitize_CpfComMascara_ESubstituido()
    {
        // Arrange
        const string entrada = "CPF do paciente: 123.456.789-09";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("123.456.789-09"));
        Assert.That(resultado, Does.Contain("[CPF_REDACTED]"));
    }

    [Test]
    public void Sanitize_CpfSemMascara_ESubstituido()
    {
        // Arrange — CPF sem pontuação como aparece em alguns campos de banco.
        const string entrada = "Documento: 12345678909 registrado.";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("12345678909"));
        Assert.That(resultado, Does.Contain("[CPF_REDACTED]"));
    }

    [Test]
    public void Sanitize_CpfParcialmenteMascarado_ESubstituido()
    {
        // Arrange — formatos mistos como 123.456.78909
        const string entrada = "CPF: 123.456.789-09 e nome: João Silva";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("123.456.789-09"));
        Assert.That(resultado, Does.Contain("João Silva"), "Texto não-PII adjacente deve ser preservado.");
    }

    [Test]
    public void Sanitize_MultiplosCpfs_TodosSaoSubstituidos()
    {
        // Arrange
        const string entrada = "CPF1: 111.222.333-44 e CPF2: 555.666.777-88";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("111.222.333-44"));
        Assert.That(resultado, Does.Not.Contain("555.666.777-88"));
        Assert.That(resultado.Split("[CPF_REDACTED]").Length - 1, Is.EqualTo(2),
            "Devem existir exatamente 2 substituições de CPF.");
    }

    // ---- CNPJ ----

    [Test]
    public void Sanitize_CnpjComMascara_ESubstituido()
    {
        // Arrange
        const string entrada = "CNPJ da clínica: 12.345.678/0001-99";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("12.345.678/0001-99"));
        Assert.That(resultado, Does.Contain("[CNPJ_REDACTED]"));
    }

    [Test]
    public void Sanitize_CnpjSemMascara_ESubstituido()
    {
        // Arrange
        const string entrada = "CNPJ: 12345678000199";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("12345678000199"));
        Assert.That(resultado, Does.Contain("[CNPJ_REDACTED]"));
    }

    /// <summary>
    /// CNPJ alfanumérico (IN RFB 2.229/2024) com máscara deve ser redactado — CA9 LGPD.
    /// Garante que a regex [A-Z0-9] cobre o formato novo, não só o numérico clássico.
    /// </summary>
    [Test]
    public void Sanitize_CnpjAlfanumericoComMascara_ESubstituido()
    {
        // Arrange — CNPJ alfanumérico válido (DVs corretos, espelhado no CnpjValidatorTests).
        const string entrada = "CNPJ alfanumérico: 12.ABC.345/01DE-35 — dados do fornecedor.";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("12.ABC.345/01DE-35"));
        Assert.That(resultado, Does.Contain("[CNPJ_REDACTED]"));
        Assert.That(resultado, Does.Contain("dados do fornecedor."), "Texto adjacente deve ser preservado.");
    }

    [Test]
    public void Sanitize_CnpjAlfanumericoSemMascara_ESubstituido()
    {
        // Arrange — forma canônica (sem pontuação) do CNPJ alfanumérico.
        const string entrada = "Fornecedor: 12ABC34501DE35";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("12ABC34501DE35"));
        Assert.That(resultado, Does.Contain("[CNPJ_REDACTED]"));
    }

    // ---- E-mail ----

    [Test]
    public void Sanitize_Email_ESubstituido()
    {
        // Arrange
        const string entrada = "Contato: paciente@clinica.com.br para mais informações.";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("paciente@clinica.com.br"));
        Assert.That(resultado, Does.Contain("[EMAIL_REDACTED]"));
        Assert.That(resultado, Does.Contain("para mais informações."), "Texto adjacente deve ser preservado.");
    }

    [Test]
    public void Sanitize_EmailComSubdominio_ESubstituido()
    {
        // Arrange
        const string entrada = "admin@sub.imedto.com.br";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("admin@sub.imedto.com.br"));
        Assert.That(resultado, Does.Contain("[EMAIL_REDACTED]"));
    }

    [Test]
    public void Sanitize_EmailComCaracteresEspeciaisPermitidos_ESubstituido()
    {
        // Arrange — e-mails válidos com +, ., _
        const string entrada = "usuario.nome+tag_123@empresa-saude.med.br";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("usuario.nome+tag_123@empresa-saude.med.br"));
        Assert.That(resultado, Does.Contain("[EMAIL_REDACTED]"));
    }

    // ---- Telefone ----

    [Test]
    public void Sanitize_TelefoneFixoComMascara_ESubstituido()
    {
        // Arrange — fixo: (11) 3333-4444
        const string entrada = "Telefone: (11) 3333-4444";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("(11) 3333-4444"));
        Assert.That(resultado, Does.Contain("[TELEFONE_REDACTED]"));
    }

    [Test]
    public void Sanitize_TelefoneCelularComMascara_ESubstituido()
    {
        // Arrange — celular: (11) 99999-8888
        const string entrada = "Cel: (11) 99999-8888 — paciente.";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("99999-8888"));
        Assert.That(resultado, Does.Contain("[TELEFONE_REDACTED]"));
        Assert.That(resultado, Does.Contain("paciente."), "Texto adjacente deve ser preservado.");
    }

    [Test]
    public void Sanitize_TelefoneCelularSemMascara11Digitos_ESubstituido()
    {
        // Arrange — celular sem máscara: 11999998888 (DDD + 9 obrigatório + 8 dígitos).
        // Regra ANATEL: celulares brasileiros pós-2014 têm o "9" obrigatório no 3º dígito.
        // O regex de telefone celular exige esse "9", o que o diferencia de CPF cru (que
        // raramente terá "9" exatamente na 3ª posição da string completa).
        const string entrada = "Fone: 11999998888";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("11999998888"));
        Assert.That(resultado, Does.Contain("[TELEFONE_REDACTED]"));
    }

    [Test]
    public void Sanitize_CpfCruNaoEConfundidoComTelefone()
    {
        // Arrange — CPF cru "12345678909" tem "3" na 3ª posição, então não casa com o
        // regex de celular (que exige "9"). Garante que CPFs sem máscara continuam sendo
        // capturados como CPF mesmo após a reordenação telefone-antes-de-CPF.
        const string entrada = "Documento: 12345678909 registrado.";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("12345678909"));
        Assert.That(resultado, Does.Contain("[CPF_REDACTED]"));
        Assert.That(resultado, Does.Not.Contain("[TELEFONE_REDACTED]"));
    }

    // ---- CEP ----

    [Test]
    public void Sanitize_CepComHifen_ESubstituido()
    {
        // Arrange
        const string entrada = "Endereço: Rua das Flores, CEP 01310-100, São Paulo";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("01310-100"));
        Assert.That(resultado, Does.Contain("[CEP_REDACTED]"));
        Assert.That(resultado, Does.Contain("Rua das Flores"), "Texto adjacente deve ser preservado.");
    }

    [Test]
    public void Sanitize_CepSemHifen_ESubstituido()
    {
        // Arrange
        const string entrada = "CEP: 01310100";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("01310100"));
        Assert.That(resultado, Does.Contain("[CEP_REDACTED]"));
    }

    // ---- RG ----

    [Test]
    public void Sanitize_RgComMascara_ESubstituido()
    {
        // Arrange
        const string entrada = "RG: 12.345.678-9";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("12.345.678-9"));
        Assert.That(resultado, Does.Contain("[RG_REDACTED]"));
    }

    [Test]
    public void Sanitize_RgComDigitoVerificadorX_ESubstituido()
    {
        // Arrange — RG com X como dígito verificador.
        const string entrada = "RG do responsável: 1.234.567-X";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("1.234.567-X"));
        Assert.That(resultado, Does.Contain("[RG_REDACTED]"));
    }

    // ---- Texto sem PII ----

    [Test]
    public void Sanitize_TextoSemPii_EPreservadoIntegralmente()
    {
        // Arrange — texto clínico legítimo sem nenhum dado pessoal identificável.
        const string entrada = "Paciente relata dor lombar há 3 dias. Sem febre. PA: 120x80 mmHg.";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Is.EqualTo(entrada));
    }

    [Test]
    public void Sanitize_TextoSoComEspacos_EPreservado()
    {
        // Arrange
        const string entrada = "   ";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Is.EqualTo(entrada));
    }

    // ---- Múltiplos tipos de PII no mesmo texto ----

    [Test]
    public void Sanitize_MultiplosTiposDePii_TodosSaoSubstituidos()
    {
        // Arrange — cenário realista: prontuário copiado com vários campos PII.
        const string entrada =
            "Nome: Maria Santos, CPF: 123.456.789-09, " +
            "e-mail: maria@gmail.com, tel: (21) 98765-4321, CEP: 20040-020";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("123.456.789-09"));
        Assert.That(resultado, Does.Not.Contain("maria@gmail.com"));
        Assert.That(resultado, Does.Not.Contain("98765-4321"));
        Assert.That(resultado, Does.Not.Contain("20040-020"));

        Assert.That(resultado, Does.Contain("[CPF_REDACTED]"));
        Assert.That(resultado, Does.Contain("[EMAIL_REDACTED]"));
        Assert.That(resultado, Does.Contain("[TELEFONE_REDACTED]"));
        Assert.That(resultado, Does.Contain("[CEP_REDACTED]"));

        // Nome "Maria Santos" não é coberto pelo sanitizer (é intencionalmente não-redactado).
        Assert.That(resultado, Does.Contain("Nome: Maria Santos"));
    }

    // ---- CNPJ tem prioridade sobre CPF (ordem de aplicação) ----

    [Test]
    public void Sanitize_CnpjNaoEConfundidoComCpf()
    {
        // Arrange — CNPJ tem mais dígitos que CPF. O regex de CPF não deve engolir
        // os primeiros 11 dígitos de um CNPJ e deixar o restante exposto.
        const string entrada = "CNPJ: 12.345.678/0001-99";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert — deve aparecer como CNPJ redactado, não CPF + sobra.
        Assert.That(resultado, Does.Contain("[CNPJ_REDACTED]"));
        // Nenhum dígito do CNPJ original deve sobrar exposto.
        Assert.That(resultado, Does.Not.Contain("12.345.678"));
        Assert.That(resultado, Does.Not.Contain("0001-99"));
    }

    // ---- String muito longa ----

    [Test]
    public void Sanitize_StringMuitoLonga_ProcessaCorreamente()
    {
        // Arrange — 50 KB de texto com um CPF no meio.
        var prefixo = new string('A', 25_000);
        var sufixo = new string('B', 25_000);
        var entrada = $"{prefixo} CPF: 987.654.321-00 {sufixo}";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Not.Contain("987.654.321-00"));
        Assert.That(resultado, Does.Contain("[CPF_REDACTED]"));
        // Verifica que o restante do texto longo foi preservado.
        Assert.That(resultado.Length, Is.GreaterThan(45_000));
    }

    // ---- Texto adjacente ao PII é preservado ----

    [Test]
    public void Sanitize_TextoAntesDoCpf_NaoECorrompido()
    {
        // Arrange
        const string entrada = "Diagnóstico: suspeita de hipertensão. CPF: 111.222.333-44. Retorno em 30 dias.";

        // Act
        var resultado = PiiSanitizer.Sanitize(entrada);

        // Assert
        Assert.That(resultado, Does.Contain("Diagnóstico: suspeita de hipertensão."));
        Assert.That(resultado, Does.Contain("Retorno em 30 dias."));
        Assert.That(resultado, Does.Contain("[CPF_REDACTED]"));
    }
}
