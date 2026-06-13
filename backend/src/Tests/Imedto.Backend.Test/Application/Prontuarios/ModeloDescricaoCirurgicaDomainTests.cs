using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

/// <summary>
/// Testes de domínio para <see cref="ModeloDescricaoCirurgica"/>:
/// invariantes de factory, edição e imutabilidade do padrão-sistema.
/// </summary>
[TestFixture]
public class ModeloDescricaoCirurgicaDomainTests
{
    // ─── CriarDoEstabelecimento ─────────────────────────────────────────────────

    [Test]
    public void CriarDoEstabelecimento_TituloECorpoValidos_CriaAtivo()
    {
        var m = ModeloDescricaoCirurgica.CriarDoEstabelecimento(1, "Rinoplastia", "Técnica aberta...");

        Assert.Multiple(() =>
        {
            Assert.That(m.EstabelecimentoId, Is.EqualTo(1));
            Assert.That(m.Titulo, Is.EqualTo("Rinoplastia"));
            Assert.That(m.Corpo, Is.EqualTo("Técnica aberta..."));
            Assert.That(m.Ativo, Is.True);
            Assert.That(m.EhPadraoSistema, Is.False);
        });
    }

    [Test]
    public void CriarDoEstabelecimento_TituloComEspacos_Trimado()
    {
        var m = ModeloDescricaoCirurgica.CriarDoEstabelecimento(1, "  Rinoplastia  ", "texto");
        Assert.That(m.Titulo, Is.EqualTo("Rinoplastia"));
    }

    [Test]
    public void CriarDoEstabelecimento_TituloVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ModeloDescricaoCirurgica.CriarDoEstabelecimento(1, "", "texto"));
        Assert.That(ex.Message, Does.Contain("Título"));
    }

    [Test]
    public void CriarDoEstabelecimento_CorpoVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ModeloDescricaoCirurgica.CriarDoEstabelecimento(1, "Rinoplastia", ""));
        Assert.That(ex.Message, Does.Contain("Corpo"));
    }

    [Test]
    public void CriarDoEstabelecimento_TituloAcima200Chars_LancaBusinessException()
    {
        var titulo = new string('A', 201);
        var ex = Assert.Throws<BusinessException>(() =>
            ModeloDescricaoCirurgica.CriarDoEstabelecimento(1, titulo, "texto"));
        Assert.That(ex.Message, Does.Contain("200"));
    }

    [Test]
    public void CriarDoEstabelecimento_EstabelecimentoIdZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ModeloDescricaoCirurgica.CriarDoEstabelecimento(0, "Rinoplastia", "texto"));
        Assert.That(ex.Message, Does.Contain("Estabelecimento"));
    }

    // ─── CriarPadraoSistema ──────────────────────────────────────────────────────

    [Test]
    public void CriarPadraoSistema_ValoresValidos_CriaComEstabelecimentoIdNull()
    {
        var m = ModeloDescricaoCirurgica.CriarPadraoSistema("Colecistectomia Vídeo", "Incisão...");

        Assert.Multiple(() =>
        {
            Assert.That(m.EstabelecimentoId, Is.Null);
            Assert.That(m.EhPadraoSistema, Is.True);
            Assert.That(m.Ativo, Is.True);
        });
    }

    // ─── Editar ──────────────────────────────────────────────────────────────────

    [Test]
    public void Editar_TituloECorpoValidos_AtualizaDados()
    {
        var m = ModeloDescricaoCirurgica.CriarDoEstabelecimento(1, "Rinoplastia", "texto original");
        m.Editar("Rinoplastia Estruturada", "novo texto");

        Assert.Multiple(() =>
        {
            Assert.That(m.Titulo, Is.EqualTo("Rinoplastia Estruturada"));
            Assert.That(m.Corpo, Is.EqualTo("novo texto"));
            Assert.That(m.AtualizadoEm, Is.Not.Null);
        });
    }

    [Test]
    public void Editar_PadraoSistema_LancaBusinessException()
    {
        var m = ModeloDescricaoCirurgica.CriarPadraoSistema("Colecistectomia", "texto");
        var ex = Assert.Throws<BusinessException>(() => m.Editar("Outro", "texto"));
        Assert.That(ex.Message, Does.Contain("padrão do sistema"));
    }

    [Test]
    public void Editar_TituloVazio_LancaBusinessException()
    {
        var m = ModeloDescricaoCirurgica.CriarDoEstabelecimento(1, "Rinoplastia", "texto");
        var ex = Assert.Throws<BusinessException>(() => m.Editar("", "texto"));
        Assert.That(ex.Message, Does.Contain("Título"));
    }

    // ─── Inativar / Reativar ─────────────────────────────────────────────────────

    [Test]
    public void Inativar_ModeloAtivo_TornaInativo()
    {
        var m = ModeloDescricaoCirurgica.CriarDoEstabelecimento(1, "Rinoplastia", "texto");
        m.Inativar();
        Assert.That(m.Ativo, Is.False);
    }

    [Test]
    public void Inativar_ModeloJaInativo_LancaBusinessException()
    {
        var m = ModeloDescricaoCirurgica.CriarDoEstabelecimento(1, "Rinoplastia", "texto");
        m.Inativar();
        Assert.Throws<BusinessException>(() => m.Inativar());
    }

    [Test]
    public void Reativar_ModeloInativo_TornaAtivo()
    {
        var m = ModeloDescricaoCirurgica.CriarDoEstabelecimento(1, "Rinoplastia", "texto");
        m.Inativar();
        m.Reativar();
        Assert.That(m.Ativo, Is.True);
    }
}
