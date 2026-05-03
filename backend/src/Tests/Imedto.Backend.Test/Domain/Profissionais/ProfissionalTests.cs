using Imedto.Backend.Domain.Profissionais;
using Imedto.Backend.Domain.Profissionais.Events;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Profissionais;

[TestFixture]
public class ProfissionalTests
{
    private static Profissional CriarValido() =>
        Profissional.Cadastrar(
            usuarioId: Guid.NewGuid(),
            conselho: " crm ",
            uf: " sp ",
            numeroRegistro: " 12345 ",
            especialidade: " Cardiologia ",
            bio: " Médico há 20 anos ");

    // ----- Cadastrar -----

    [Test]
    public void Cadastrar_Valido_NormalizaEPublicaEvento()
    {
        var p = CriarValido();
        Assert.That(p.Conselho, Is.EqualTo("CRM"));
        Assert.That(p.Uf, Is.EqualTo("SP"));
        Assert.That(p.NumeroRegistro, Is.EqualTo("12345"));
        Assert.That(p.Especialidade, Is.EqualTo("Cardiologia"));
        Assert.That(p.Bio, Is.EqualTo("Médico há 20 anos"));
        Assert.That(p.CriadoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
        Assert.That(p.DomainEvents.OfType<ProfissionalCadastradoEvent>(), Has.Exactly(1).Items);
    }

    [Test]
    public void Cadastrar_UsuarioEmpty_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Profissional.Cadastrar(Guid.Empty, "CRM", "SP", "1", null, null));
        Assert.That(ex.Message, Does.Contain("Usuário"));
    }

    [Test]
    public void Cadastrar_ConselhoVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Profissional.Cadastrar(Guid.NewGuid(), "  ", "SP", "1", null, null));
        Assert.That(ex.Message, Does.Contain("Conselho"));
    }

    [Test]
    public void Cadastrar_UfTamanhoErrado_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Profissional.Cadastrar(Guid.NewGuid(), "CRM", "SAO", "1", null, null));
        Assert.That(ex.Message, Does.Contain("UF"));
    }

    [Test]
    public void Cadastrar_NumeroVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Profissional.Cadastrar(Guid.NewGuid(), "CRM", "SP", " ", null, null));
        Assert.That(ex.Message, Does.Contain("Número"));
    }

    [Test]
    public void Cadastrar_SemEspecialidadeOuBio_PermiteNull()
    {
        var p = Profissional.Cadastrar(Guid.NewGuid(), "CRM", "SP", "1", " ", "");
        Assert.That(p.Especialidade, Is.Null);
        Assert.That(p.Bio, Is.Null);
    }

    // ----- Atualizar -----

    [Test]
    public void Atualizar_Valido_AtualizaCampos()
    {
        var p = CriarValido();
        p.Atualizar("CRO", "RJ", "999", "Ortodontia", "Nova bio");

        Assert.That(p.Conselho, Is.EqualTo("CRO"));
        Assert.That(p.Uf, Is.EqualTo("RJ"));
        Assert.That(p.NumeroRegistro, Is.EqualTo("999"));
        Assert.That(p.Especialidade, Is.EqualTo("Ortodontia"));
        Assert.That(p.Bio, Is.EqualTo("Nova bio"));
        Assert.That(p.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void Atualizar_ConselhoVazio_LancaBusinessException()
    {
        var p = CriarValido();
        Assert.Throws<BusinessException>(() => p.Atualizar(" ", "SP", "1", null, null));
    }

    // ----- AlterarFoto -----

    [Test]
    public void AlterarFoto_UrlValida_AtualizaFoto()
    {
        var p = CriarValido();
        p.AlterarFoto("https://cdn/foto.png");
        Assert.That(p.FotoUrl, Is.EqualTo("https://cdn/foto.png"));
        Assert.That(p.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void AlterarFoto_UrlVazia_LancaBusinessException()
    {
        var p = CriarValido();
        Assert.Throws<BusinessException>(() => p.AlterarFoto(" "));
    }

    // ----- MarcarComoDeletado -----

    [Test]
    public void MarcarComoDeletado_Valido_SetaAuditoria()
    {
        var p = CriarValido();
        var quem = Guid.NewGuid();
        p.MarcarComoDeletado(quem);

        Assert.That(p.EstaDeletado, Is.True);
        Assert.That(p.DeletadoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
        Assert.That(p.DeletadoPorUsuarioId, Is.EqualTo(quem));
    }

    [Test]
    public void MarcarComoDeletado_UsuarioEmpty_LancaBusinessException()
    {
        var p = CriarValido();
        Assert.Throws<BusinessException>(() => p.MarcarComoDeletado(Guid.Empty));
    }

    [Test]
    public void MarcarComoDeletado_JaDeletado_LancaBusinessException()
    {
        var p = CriarValido();
        p.MarcarComoDeletado(Guid.NewGuid());
        Assert.Throws<BusinessException>(() => p.MarcarComoDeletado(Guid.NewGuid()));
    }
}
