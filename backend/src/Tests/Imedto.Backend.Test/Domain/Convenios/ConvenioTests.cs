using Imedto.Backend.Domain.Convenios;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Convenios;

[TestFixture]
public class ConvenioTests
{
    private const long EstabId = 1L;

    // ── Invariantes do aggregate ──────────────────────────────────────────────

    [Test]
    public void Criar_EstabelecimentoZero_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Convenio.Criar(0, "Unimed", null));
        Assert.That(ex.Message, Does.Contain("Estabelecimento"));
    }

    [Test]
    public void Criar_NomeVazio_Lanca()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Convenio.Criar(EstabId, "  ", null));
        Assert.That(ex.Message, Does.Contain("Nome"));
    }

    [Test]
    public void Criar_NomeValido_CriaAtivo()
    {
        var c = Convenio.Criar(EstabId, " Unimed ", "123456");
        Assert.Multiple(() =>
        {
            Assert.That(c.Nome, Is.EqualTo("Unimed"));
            Assert.That(c.RegistroAns, Is.EqualTo("123456"));
            Assert.That(c.Ativo, Is.True);
            Assert.That(c.EstabelecimentoId, Is.EqualTo(EstabId));
        });
    }

    [Test]
    public void Criar_RegistroAnsVazio_FicaNulo()
    {
        var c = Convenio.Criar(EstabId, "Unimed", "  ");
        Assert.That(c.RegistroAns, Is.Null);
    }

    [Test]
    public void Inativar_TornaInativo()
    {
        var c = Convenio.Criar(EstabId, "Unimed", null);
        c.Inativar();
        Assert.That(c.Ativo, Is.False);
        Assert.That(c.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void Reativar_TornaAtivo()
    {
        var c = Convenio.Criar(EstabId, "Unimed", null);
        c.Inativar();
        c.Reativar();
        Assert.That(c.Ativo, Is.True);
    }

    [Test]
    public void Atualizar_NomeVazio_Lanca()
    {
        var c = Convenio.Criar(EstabId, "Unimed", null);
        var ex = Assert.Throws<BusinessException>(() => c.Atualizar("", null));
        Assert.That(ex.Message, Does.Contain("Nome"));
    }

    // ── Planos via root ───────────────────────────────────────────────────────

    [Test]
    public void AdicionarPlano_NomeValido_AdicionaAoRoot()
    {
        var c = Convenio.Criar(EstabId, "Unimed", null);
        var plano = c.AdicionarPlano("Unimed Fácil");
        Assert.That(c.Planos, Has.Count.EqualTo(1));
        Assert.That(plano.Nome, Is.EqualTo("Unimed Fácil"));
        Assert.That(plano.Ativo, Is.True);
    }

    [Test]
    public void AdicionarPlano_NomeVazio_Lanca()
    {
        var c = Convenio.Criar(EstabId, "Unimed", null);
        var ex = Assert.Throws<BusinessException>(() => c.AdicionarPlano("  "));
        Assert.That(ex.Message, Does.Contain("Nome do plano"));
    }

    [Test]
    public void InativarPlano_PlanoExistente_TornaInativo()
    {
        var c = Convenio.Criar(EstabId, "Unimed", null);
        // Simula Id pós-persistência (0 é válido para teste de domínio inline)
        var plano = c.AdicionarPlano("Enfermaria");
        // InativarPlano por objeto direto (Id=0 em memória)
        c.InativarPlano(plano.Id);
        Assert.That(plano.Ativo, Is.False);
    }

    [Test]
    public void InativarPlano_PlanoInexistente_Lanca()
    {
        var c = Convenio.Criar(EstabId, "Unimed", null);
        var ex = Assert.Throws<BusinessException>(() => c.InativarPlano(9999));
        Assert.That(ex.Message, Does.Contain("Plano não encontrado"));
    }

    [Test]
    public void AtualizarPlano_PlanoExistente_MudaNome()
    {
        var c = Convenio.Criar(EstabId, "Unimed", null);
        var plano = c.AdicionarPlano("Antigo");
        c.AtualizarPlano(plano.Id, "Novo Nome");
        Assert.That(plano.Nome, Is.EqualTo("Novo Nome"));
    }

    [Test]
    public void AtualizarPlano_PlanoInexistente_Lanca()
    {
        var c = Convenio.Criar(EstabId, "Unimed", null);
        var ex = Assert.Throws<BusinessException>(() => c.AtualizarPlano(9999, "X"));
        Assert.That(ex.Message, Does.Contain("Plano não encontrado"));
    }
}
