using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.Test.Helpers;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Pacientes;

/// <summary>
/// Unit tests da regra de documento do aggregate Paciente:
/// — CPF (com DV) OU DocumentoInternacional, nunca os dois;
/// — CPF eh OPCIONAL — Cadastrar com null funciona;
/// — DV invalido lanca BusinessException("CPF invalido.");
/// — DocumentoInternacional limitado a 30 chars + trim;
/// — Anonimizar zera AMBOS os campos.
/// </summary>
[TestFixture]
public class PacienteDocumentoTests
{
    private static Paciente CadastrarComCpf(string cpf) =>
        Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Joao Silva",
            cpf: cpf,
            dataNascimento: null,
            genero: GeneroPaciente.NaoInformado,
            telefone: null,
            email: null,
            endereco: null,
            observacoes: null);

    private static Paciente CadastrarComDocInternacional(string doc) =>
        Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Estrangeiro Teste",
            cpf: null,
            dataNascimento: null,
            genero: GeneroPaciente.NaoInformado,
            telefone: null,
            email: null,
            endereco: null,
            observacoes: null,
            documentoInternacional: doc);

    // --- CPF ---

    [Test]
    public void Cadastrar_ComCpfValido_GuardaApenasDigitos()
    {
        // CpfTestData.Validos[0] = 12345678909 (DV correto)
        var p = CadastrarComCpf("123.456.789-09");

        Assert.That(p.Cpf, Is.EqualTo("12345678909"));
        Assert.That(p.DocumentoInternacional, Is.Null);
    }

    [Test]
    public void Cadastrar_SemCpf_PersisteNull()
    {
        var p = CadastrarComCpf(null);

        Assert.That(p.Cpf, Is.Null);
        Assert.That(p.DocumentoInternacional, Is.Null);
    }

    [TestCase("12345678900", "dígito")]      // DV2 errado
    [TestCase("11111111111", "sequência")]   // sequência repetida
    [TestCase("123",         "11 dígitos")]  // curto demais
    public void Cadastrar_CpfInvalido_LancaBusinessException(string cpf, string razaoEsperada)
    {
        var ex = Assert.Throws<BusinessException>(() => CadastrarComCpf(cpf));
        // Mensagens granulares (RazaoInvalidez do CpfValidator) — informam o motivo
        // específico em vez de um "CPF inválido" genérico.
        Assert.That(ex.Message, Does.Contain(razaoEsperada).IgnoreCase,
            $"Esperava razão '{razaoEsperada}' para CPF '{cpf}', recebi: '{ex.Message}'");
    }

    // --- DocumentoInternacional ---

    [Test]
    public void Cadastrar_ComDocInternacional_GuardaTrim()
    {
        var p = CadastrarComDocInternacional("  PASSAPORTE-AB123456  ");

        Assert.That(p.DocumentoInternacional, Is.EqualTo("PASSAPORTE-AB123456"));
        Assert.That(p.Cpf, Is.Null);
    }

    [Test]
    public void Cadastrar_DocInternacionalAcima30Chars_LancaBusinessException()
    {
        var doc = new string('A', 31);

        var ex = Assert.Throws<BusinessException>(() => CadastrarComDocInternacional(doc));
        Assert.That(ex.Message, Does.Contain("30").And.Contain("internacional").IgnoreCase);
    }

    [Test]
    public void Cadastrar_DocInternacionalNoLimiteDe30_Permitido()
    {
        var doc = new string('A', 30);
        var p = CadastrarComDocInternacional(doc);

        Assert.That(p.DocumentoInternacional, Is.EqualTo(doc));
    }

    // --- Regra exclusiva ---

    [Test]
    public void Cadastrar_AmbosOsCamposPreenchidos_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() => Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Conflito",
            cpf: CpfTestData.Validos[0],
            dataNascimento: null,
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null,
            documentoInternacional: "PASSAPORTE-X1"));

        Assert.That(ex.Message, Does.Contain("apenas um").IgnoreCase);
    }

    // --- AtualizarDados ---

    [Test]
    public void AtualizarDados_TrocaCpfPorDocInternacional_PersisteCorrigido()
    {
        var p = CadastrarComCpf(CpfTestData.Validos[0]);

        p.AtualizarDados(
            nomeCompleto: p.NomeCompleto,
            cpf: null,
            dataNascimento: p.DataNascimento,
            genero: p.Genero,
            telefone: p.Telefone, email: p.Email, endereco: p.Endereco, observacoes: p.Observacoes,
            documentoInternacional: "PASSAPORTE-X1");

        Assert.That(p.Cpf, Is.Null);
        Assert.That(p.DocumentoInternacional, Is.EqualTo("PASSAPORTE-X1"));
    }

    [Test]
    public void AtualizarDados_CpfInvalido_LancaBusinessException()
    {
        var p = CadastrarComCpf(CpfTestData.Validos[0]);

        var ex = Assert.Throws<BusinessException>(() => p.AtualizarDados(
            nomeCompleto: p.NomeCompleto,
            cpf: "99999999999", // sequencia repetida
            dataNascimento: null, genero: p.Genero,
            telefone: null, email: null, endereco: null, observacoes: null));

        Assert.That(ex.Message, Does.Contain("CPF").IgnoreCase);
    }

    // --- Anonimizacao ---

    [Test]
    public void Anonimizar_ComDocInternacional_LimpaCampo()
    {
        var p = CadastrarComDocInternacional("PASSAPORTE-AB123456");

        p.Anonimizar(Guid.NewGuid());

        Assert.That(p.DocumentoInternacional, Is.Null);
        Assert.That(p.Cpf, Is.Null);
        Assert.That(p.EstaAnonimizado, Is.True);
    }
}
