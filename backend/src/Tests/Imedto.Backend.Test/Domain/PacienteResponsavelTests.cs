using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain;

/// <summary>
/// Testes de domínio para as regras de responsável do paciente (briefing 2026-06-23_002):
/// R3 (obrigatoriedade para menor), R4 (opcional para maior), R8 (anonimização),
/// borda dos 18 anos (CA5) e borda dos 60 anos (CA6).
/// </summary>
[TestFixture]
public class PacienteResponsavelTests
{
    private static DateTime HojeUtc => DateTime.UtcNow.Date;

    // Calcula data de nascimento que resulta em exatamente N anos hoje
    private static DateTime NascComIdade(int anos) =>
        HojeUtc.AddYears(-anos);

    // Calcula data de nascimento que resulta em exatamente N anos amanhã (hoje = N-1 anos)
    private static DateTime NascComIdadeAmanha(int anos) =>
        HojeUtc.AddYears(-anos).AddDays(1);

    private static Paciente CriarMenorSemResponsavel(DateTime dataNasc)
    {
        return Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Menor Teste",
            cpf: null,
            dataNascimento: dataNasc,
            genero: GeneroPaciente.NaoInformado,
            telefone: null,
            email: null,
            endereco: null,
            observacoes: null);
    }

    #region CA11 — Menor sem responsável deve lançar 422

    [Test]
    public void Cadastrar_MenorSemResponsavel_LancaBusinessException()
    {
        var nascimento = NascComIdade(10); // 10 anos
        Assert.Throws<BusinessException>(() => Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Criança Teste",
            cpf: null,
            dataNascimento: nascimento,
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null,
            responsavelNome: null,
            responsavelParentesco: null));
    }

    [Test]
    public void Cadastrar_MenorSemParentesco_LancaBusinessException()
    {
        var nascimento = NascComIdade(15);
        Assert.Throws<BusinessException>(() => Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Adolescente Teste",
            cpf: null,
            dataNascimento: nascimento,
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null,
            responsavelNome: "Maria Souza",
            responsavelParentesco: null));
    }

    [Test]
    public void Cadastrar_MenorSemNomeResponsavel_LancaBusinessException()
    {
        var nascimento = NascComIdade(17);
        Assert.Throws<BusinessException>(() => Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Menor 17",
            cpf: null,
            dataNascimento: nascimento,
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null,
            responsavelNome: null,
            responsavelParentesco: "Mãe"));
    }

    [Test]
    public void Cadastrar_MenorComNomeEParentesco_Sucesso()
    {
        var nascimento = NascComIdade(10);
        var paciente = Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Criança OK",
            cpf: null,
            dataNascimento: nascimento,
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null,
            responsavelNome: "João Silva",
            responsavelParentesco: "Pai",
            responsavelTelefone: null);

        Assert.That(paciente.ResponsavelNome, Is.EqualTo("João Silva"));
        Assert.That(paciente.ResponsavelParentesco, Is.EqualTo("Pai"));
        Assert.That(paciente.ResponsavelTelefone, Is.Null);
    }

    [Test]
    public void Cadastrar_MenorComTodosResponsavel_Sucesso()
    {
        var nascimento = NascComIdade(8);
        var paciente = Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Criança Completa",
            cpf: null,
            dataNascimento: nascimento,
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null,
            responsavelNome: "Ana Lima",
            responsavelParentesco: "Mãe",
            responsavelTelefone: "11987654321");

        Assert.That(paciente.ResponsavelNome, Is.EqualTo("Ana Lima"));
        Assert.That(paciente.ResponsavelParentesco, Is.EqualTo("Mãe"));
        // Telefone saneado para dígitos
        Assert.That(paciente.ResponsavelTelefone, Is.EqualTo("11987654321"));
    }

    #endregion

    #region CA5 — Borda dos 18 anos

    [Test]
    public void Cadastrar_PacienteCompleta18AnosHoje_NaoExigeResponsavel()
    {
        // No dia em que completa 18, já é adulto — não exige responsável
        var nascimento = NascComIdade(18);
        Assert.DoesNotThrow(() => Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Aniversário 18",
            cpf: null,
            dataNascimento: nascimento,
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null));
    }

    [Test]
    public void Cadastrar_PacienteCompleta18AnosAmanha_ExigeResponsavel()
    {
        // Falta 1 dia para 18 → ainda é menor
        var nascimento = NascComIdadeAmanha(18);
        Assert.Throws<BusinessException>(() => Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Quase 18",
            cpf: null,
            dataNascimento: nascimento,
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null));
    }

    #endregion

    #region CA9 — Maior de idade sem responsável deve ser aceito

    [Test]
    public void Cadastrar_MaiorDeIdadeSemResponsavel_Sucesso()
    {
        var nascimento = NascComIdade(30);
        var paciente = Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Adulto Teste",
            cpf: null,
            dataNascimento: nascimento,
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null);

        Assert.That(paciente.ResponsavelNome, Is.Null);
        Assert.That(paciente.ResponsavelParentesco, Is.Null);
        Assert.That(paciente.ResponsavelTelefone, Is.Null);
    }

    [Test]
    public void Cadastrar_SemDataNascimento_NaoExigeResponsavel()
    {
        var paciente = Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Sem Data",
            cpf: null,
            dataNascimento: null,
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null);

        Assert.That(paciente.ResponsavelNome, Is.Null);
    }

    #endregion

    #region CA11 via AtualizarDados

    [Test]
    public void AtualizarDados_MenorSemResponsavel_LancaBusinessException()
    {
        var paciente = Paciente.Cadastrar(
            estabelecimentoId: 1, nomeCompleto: "Adulto", cpf: null,
            dataNascimento: NascComIdade(30), genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null);

        // Atualiza para menor sem responsável — deve lançar
        Assert.Throws<BusinessException>(() => paciente.AtualizarDados(
            nomeCompleto: "Adulto Virado Menor",
            cpf: null,
            dataNascimento: NascComIdade(10),
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null));
    }

    [Test]
    public void AtualizarDados_MenorComResponsavel_Sucesso()
    {
        var paciente = Paciente.Cadastrar(
            estabelecimentoId: 1, nomeCompleto: "Adulto", cpf: null,
            dataNascimento: NascComIdade(30), genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null);

        Assert.DoesNotThrow(() => paciente.AtualizarDados(
            nomeCompleto: "Agora Menor",
            cpf: null,
            dataNascimento: NascComIdade(12),
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null,
            responsavelNome: "Carlos Pai",
            responsavelParentesco: "Pai"));

        Assert.That(paciente.ResponsavelNome, Is.EqualTo("Carlos Pai"));
    }

    #endregion

    #region CA20 — Anonimização limpa responsável

    [Test]
    public void Anonimizar_LimpaResponsavel()
    {
        var paciente = Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Adulto Com Responsável",
            cpf: null,
            dataNascimento: NascComIdade(30),
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null,
            responsavelNome: "Familiar",
            responsavelParentesco: "Cônjuge");

        paciente.Anonimizar(usuarioId: Guid.NewGuid());

        Assert.That(paciente.ResponsavelNome, Is.Null);
        Assert.That(paciente.ResponsavelParentesco, Is.Null);
        Assert.That(paciente.ResponsavelTelefone, Is.Null);
    }

    #endregion

    #region R5 — Saneamento do telefone (dígitos)

    [Test]
    public void Cadastrar_TelefoneMascarado_SaneadoParaDigitos()
    {
        var paciente = Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Menor Fone Mascarado",
            cpf: null,
            dataNascimento: NascComIdade(10),
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null,
            responsavelNome: "Maria",
            responsavelParentesco: "Mãe",
            responsavelTelefone: "(11) 98765-4321");

        Assert.That(paciente.ResponsavelTelefone, Is.EqualTo("11987654321"));
    }

    [Test]
    public void Cadastrar_TelefoneVazio_NuloNoAggregate()
    {
        var paciente = Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Menor Sem Fone",
            cpf: null,
            dataNascimento: NascComIdade(5),
            genero: GeneroPaciente.NaoInformado,
            telefone: null, email: null, endereco: null, observacoes: null,
            responsavelNome: "João",
            responsavelParentesco: "Pai",
            responsavelTelefone: "");

        Assert.That(paciente.ResponsavelTelefone, Is.Null);
    }

    #endregion
}
