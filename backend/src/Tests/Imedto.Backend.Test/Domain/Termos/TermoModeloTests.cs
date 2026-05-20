using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Termos;

[TestFixture]
public class TermoModeloTests
{
    private const string HtmlSimples = "<p>Conteúdo do termo</p>";

    [Test]
    public void CriarDoEstabelecimento_ValoresValidos_RetornaAggregateAtivoVersao1()
    {
        var m = TermoModelo.CriarDoEstabelecimento(1, Guid.NewGuid(), CategoriaTermo.Lgpd, "Titulo válido", HtmlSimples);

        Assert.Multiple(() =>
        {
            Assert.That(m.EstabelecimentoId, Is.EqualTo(1));
            Assert.That(m.Categoria, Is.EqualTo(CategoriaTermo.Lgpd));
            Assert.That(m.Titulo, Is.EqualTo("Titulo válido"));
            Assert.That(m.ConteudoHtml, Is.EqualTo(HtmlSimples));
            Assert.That(m.Ativo, Is.True);
            Assert.That(m.VersaoAtual, Is.EqualTo(1));
            Assert.That(m.EhPadraoDoSistema, Is.False);
        });
    }

    [Test]
    public void CriarDoEstabelecimento_EstabIdInvalido_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            TermoModelo.CriarDoEstabelecimento(0, Guid.NewGuid(), CategoriaTermo.Geral, "Titulo", HtmlSimples));
    }

    [Test]
    public void CriarDoEstabelecimento_UsuarioVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            TermoModelo.CriarDoEstabelecimento(1, Guid.Empty, CategoriaTermo.Geral, "Titulo", HtmlSimples));
    }

    [Test]
    public void CriarDoEstabelecimento_TituloVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            TermoModelo.CriarDoEstabelecimento(1, Guid.NewGuid(), CategoriaTermo.Geral, "   ", HtmlSimples));
    }

    [Test]
    public void CriarDoEstabelecimento_TituloMuitoCurto_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            TermoModelo.CriarDoEstabelecimento(1, Guid.NewGuid(), CategoriaTermo.Geral, "ab", HtmlSimples));
    }

    [Test]
    public void CriarDoEstabelecimento_TituloMuitoLongo_LancaBusinessException()
    {
        var longo = new string('x', TermoModelo.TituloMaximo + 1);
        Assert.Throws<BusinessException>(() =>
            TermoModelo.CriarDoEstabelecimento(1, Guid.NewGuid(), CategoriaTermo.Geral, longo, HtmlSimples));
    }

    [Test]
    public void CriarDoEstabelecimento_ConteudoVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            TermoModelo.CriarDoEstabelecimento(1, Guid.NewGuid(), CategoriaTermo.Geral, "Titulo válido", ""));
    }

    [Test]
    public void CriarDoEstabelecimento_ConteudoExcedeLimite_LancaBusinessException()
    {
        // > 200 KB
        var grande = new string('a', TermoModelo.ConteudoHtmlMaximoBytes + 100);
        Assert.Throws<BusinessException>(() =>
            TermoModelo.CriarDoEstabelecimento(1, Guid.NewGuid(), CategoriaTermo.Geral, "Titulo válido", grande));
    }

    [Test]
    public void CriarPadraoDoSistema_RetornaEstabelecimentoIdNuloEEhPadraoDoSistema()
    {
        var m = TermoModelo.CriarPadraoDoSistema(CategoriaTermo.Lgpd, "Padrão LGPD", HtmlSimples);
        Assert.That(m.EstabelecimentoId, Is.Null);
        Assert.That(m.EhPadraoDoSistema, Is.True);
    }

    [Test]
    public void ClonarDePadrao_PreservaCategoriaTituloConteudoEReferenciaPadrao()
    {
        var padrao = TermoModelo.CriarPadraoDoSistema(CategoriaTermo.Telemedicina, "Padrão telemedicina", HtmlSimples);
        // Padrão precisa de Id pra clonar (fluxo real persiste antes)
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(padrao, 99L);

        var clone = TermoModelo.ClonarDePadrao(padrao, 1, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(clone.EhPadraoDoSistema, Is.False);
            Assert.That(clone.EstabelecimentoId, Is.EqualTo(1));
            Assert.That(clone.Categoria, Is.EqualTo(CategoriaTermo.Telemedicina));
            Assert.That(clone.Titulo, Is.EqualTo("Padrão telemedicina"));
            Assert.That(clone.PadraoClonadoDeId, Is.EqualTo(99L));
            Assert.That(clone.VersaoAtual, Is.EqualTo(1));
        });
    }

    [Test]
    public void ClonarDePadrao_ModeloDoEstabelecimento_LancaBusinessException()
    {
        var modeloDoEstab = TermoModelo.CriarDoEstabelecimento(1, Guid.NewGuid(), CategoriaTermo.Geral, "Titulo", HtmlSimples);
        Assert.Throws<BusinessException>(() =>
            TermoModelo.ClonarDePadrao(modeloDoEstab, 2, Guid.NewGuid()));
    }

    [Test]
    public void Atualizar_ConteudoMudou_BumpaVersao()
    {
        var m = TermoModelo.CriarDoEstabelecimento(1, Guid.NewGuid(), CategoriaTermo.Geral, "Titulo", HtmlSimples);

        var mudou = m.Atualizar(CategoriaTermo.Lgpd, "Outro título", "<p>Novo conteúdo</p>");

        Assert.That(mudou, Is.True);
        Assert.That(m.VersaoAtual, Is.EqualTo(2));
        Assert.That(m.Categoria, Is.EqualTo(CategoriaTermo.Lgpd));
        Assert.That(m.Titulo, Is.EqualTo("Outro título"));
    }

    [Test]
    public void Atualizar_SomenteTituloMudou_NaoBumpaVersao()
    {
        var m = TermoModelo.CriarDoEstabelecimento(1, Guid.NewGuid(), CategoriaTermo.Geral, "Titulo", HtmlSimples);

        var mudou = m.Atualizar(CategoriaTermo.Geral, "Novo título", HtmlSimples);

        Assert.That(mudou, Is.False);
        Assert.That(m.VersaoAtual, Is.EqualTo(1));
        Assert.That(m.Titulo, Is.EqualTo("Novo título"));
    }

    [Test]
    public void Atualizar_DiferencaApenasQuebraDeLinha_NaoBumpaVersao()
    {
        var m = TermoModelo.CriarDoEstabelecimento(1, Guid.NewGuid(), CategoriaTermo.Geral, "Titulo", "<p>L1</p>\n<p>L2</p>");
        var mudou = m.Atualizar(CategoriaTermo.Geral, "Titulo", "<p>L1</p>\r\n<p>L2</p>");
        Assert.That(mudou, Is.False);
        Assert.That(m.VersaoAtual, Is.EqualTo(1));
    }

    [Test]
    public void Atualizar_PadraoDoSistema_LancaBusinessException()
    {
        var padrao = TermoModelo.CriarPadraoDoSistema(CategoriaTermo.Geral, "Padrão", HtmlSimples);
        Assert.Throws<BusinessException>(() =>
            padrao.Atualizar(CategoriaTermo.Geral, "Outro", HtmlSimples));
    }

    [Test]
    public void MarcarComoDeletado_ModeloDoEstabelecimento_MarcaEEstadoInativo()
    {
        var m = TermoModelo.CriarDoEstabelecimento(1, Guid.NewGuid(), CategoriaTermo.Geral, "Titulo", HtmlSimples);
        m.MarcarComoDeletado();
        Assert.That(m.EstaDeletado, Is.True);
        Assert.That(m.Ativo, Is.False);
    }

    [Test]
    public void MarcarComoDeletado_PadraoDoSistema_LancaBusinessException()
    {
        var padrao = TermoModelo.CriarPadraoDoSistema(CategoriaTermo.Geral, "Padrão", HtmlSimples);
        Assert.Throws<BusinessException>(() => padrao.MarcarComoDeletado());
    }

    [Test]
    public void AlterarAtivo_PadraoDoSistema_LancaBusinessException()
    {
        var padrao = TermoModelo.CriarPadraoDoSistema(CategoriaTermo.Geral, "Padrão", HtmlSimples);
        Assert.Throws<BusinessException>(() => padrao.AlterarAtivo(false));
    }

    [Test]
    public void CriarSnapshotVersaoAtual_ModeloNaoPersistido_LancaInvalidOperation()
    {
        var m = TermoModelo.CriarDoEstabelecimento(1, Guid.NewGuid(), CategoriaTermo.Geral, "Titulo", HtmlSimples);
        Assert.Throws<InvalidOperationException>(() => m.CriarSnapshotVersaoAtual(Guid.NewGuid()));
    }

    [Test]
    public void CriarSnapshotVersaoAtual_ModeloPersistido_RetornaVersao1()
    {
        var m = TermoModelo.CriarDoEstabelecimento(1, Guid.NewGuid(), CategoriaTermo.Geral, "Titulo", HtmlSimples);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(m, 7L);

        var v = m.CriarSnapshotVersaoAtual(Guid.NewGuid());

        Assert.That(v.TermoModeloId, Is.EqualTo(7L));
        Assert.That(v.Versao, Is.EqualTo(1));
        Assert.That(v.ConteudoHtml, Is.EqualTo(HtmlSimples));
    }
}
