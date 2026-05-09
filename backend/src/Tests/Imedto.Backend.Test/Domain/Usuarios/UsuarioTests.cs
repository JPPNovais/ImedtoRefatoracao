using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Usuarios.Events;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Usuarios;

[TestFixture]
public class UsuarioTests
{
    private static Usuario CriarValido() =>
        Usuario.Criar(Guid.NewGuid(), "joao@imedto.com");

    // ----- Criar -----

    [Test]
    public void Criar_Valido_StatusPendente()
    {
        var u = CriarValido();
        Assert.That(u.Status, Is.EqualTo(UsuarioStatus.Pendente));
        Assert.That(u.OnboardingCompleto, Is.False);
        Assert.That(u.CriadoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void Criar_NormalizaEmail_TrimELowercase()
    {
        var u = Usuario.Criar(Guid.NewGuid(), "  Joao@IMEDTO.com  ");
        Assert.That(u.Email, Is.EqualTo("joao@imedto.com"));
    }

    [Test]
    public void Criar_PublicaUsuarioCriadoEvent()
    {
        var u = CriarValido();
        Assert.That(u.DomainEvents, Has.Count.EqualTo(1));
        Assert.That(u.DomainEvents.First(), Is.TypeOf<UsuarioCriadoEvent>());
    }

    [Test]
    public void Criar_IdGuidEmpty_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Usuario.Criar(Guid.Empty, "joao@imedto.com"));
        Assert.That(ex.Message, Does.Contain("Identificador"));
    }

    [Test]
    public void Criar_EmailVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Usuario.Criar(Guid.NewGuid(), "  "));
        Assert.That(ex.Message, Does.Contain("E-mail"));
    }

    // ----- CompletarOnboarding -----

    [Test]
    public void CompletarOnboarding_Valido_SetaTudoEAtivaConta()
    {
        var u = CriarValido();
        u.CompletarOnboarding("João da Silva", "12345678909", "11999998888");

        Assert.That(u.NomeCompleto, Is.EqualTo("João da Silva"));
        Assert.That(u.Cpf, Is.EqualTo("12345678909"));
        Assert.That(u.Telefone, Is.EqualTo("11999998888"));
        Assert.That(u.Status, Is.EqualTo(UsuarioStatus.Ativo));
        Assert.That(u.OnboardingCompleto, Is.True);
        Assert.That(u.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void CompletarOnboarding_CpfMascarado_SalvaSomenteDigitos()
    {
        var u = CriarValido();
        u.CompletarOnboarding("João", "123.456.789-09", "+55 (11) 99999-8888");
        Assert.That(u.Cpf, Is.EqualTo("12345678909"));
        Assert.That(u.Telefone, Is.EqualTo("5511999998888"));
    }

    [Test]
    public void CompletarOnboarding_NomeVazio_LancaBusinessException()
    {
        var u = CriarValido();
        var ex = Assert.Throws<BusinessException>(() =>
            u.CompletarOnboarding("  ", "12345678909", null));
        Assert.That(ex.Message, Does.Contain("Nome"));
    }

    [Test]
    public void CompletarOnboarding_CpfVazio_LancaBusinessException()
    {
        var u = CriarValido();
        var ex = Assert.Throws<BusinessException>(() =>
            u.CompletarOnboarding("João", "", null));
        Assert.That(ex.Message, Does.Contain("CPF"));
    }

    [Test]
    public void CompletarOnboarding_CpfTamanhoInvalido_LancaBusinessException()
    {
        var u = CriarValido();
        var ex = Assert.Throws<BusinessException>(() =>
            u.CompletarOnboarding("João", "12345", null));
        Assert.That(ex.Message, Does.Contain("11 dígitos"));
    }

    [Test]
    public void CompletarOnboarding_SemTelefone_PermiteNull()
    {
        var u = CriarValido();
        u.CompletarOnboarding("João", "12345678909", null);
        Assert.That(u.Telefone, Is.Null);
    }

    // ----- AtualizarPerfil -----

    [Test]
    public void AtualizarPerfil_NovoNomeETelefone_AtualizaEMantemCpf()
    {
        var u = CriarValido();
        u.CompletarOnboarding("João", "12345678909", "11999998888");
        u.AtualizarPerfil("João Silva", "11888887777");

        Assert.That(u.NomeCompleto, Is.EqualTo("João Silva"));
        Assert.That(u.Telefone, Is.EqualTo("11888887777"));
        Assert.That(u.Cpf, Is.EqualTo("12345678909"), "CPF nao deve mudar via AtualizarPerfil.");
    }

    [Test]
    public void AtualizarPerfil_NomeVazio_MantemNomeAnterior()
    {
        var u = CriarValido();
        u.CompletarOnboarding("João", "12345678909", null);
        u.AtualizarPerfil("  ", "11999998888");
        Assert.That(u.NomeCompleto, Is.EqualTo("João"));
    }

    [Test]
    public void AtualizarPerfil_TelefoneVazio_NulificaTelefone()
    {
        var u = CriarValido();
        u.CompletarOnboarding("João", "12345678909", "11999998888");
        u.AtualizarPerfil("João", "");
        Assert.That(u.Telefone, Is.Null);
    }

    // ----- RegistrarAcesso -----

    [Test]
    public void RegistrarAcesso_AtualizaUltimoAcessoEm()
    {
        var u = CriarValido();
        Assert.That(u.UltimoAcessoEm, Is.Null);

        u.RegistrarAcesso();
        Assert.That(u.UltimoAcessoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    // ----- Anonimizar (LGPD) -----

    [Test]
    public void Anonimizar_LimpaPiiEMantemEmail()
    {
        var u = CriarValido();
        var emailOriginal = u.Email;
        u.CompletarOnboarding("João da Silva", "12345678909", "11999998888");

        u.Anonimizar();

        Assert.That(u.NomeCompleto, Does.Contain("Anonimizado"));
        Assert.That(u.Cpf, Is.Null);
        Assert.That(u.Telefone, Is.Null);
        Assert.That(u.Status, Is.EqualTo(UsuarioStatus.Inativo));
        Assert.That(u.Email, Is.EqualTo(emailOriginal),
            "E-mail eh mantido para preservar identificacao na credencial de auth (controller revoga sessao depois).");
    }
}
