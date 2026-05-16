using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Orcamentos;

[TestFixture]
public class OrcamentoAnestesistaTests
{
    [Test]
    public void Criar_ComNome_PreencheCamposBasicos()
    {
        var sut = OrcamentoAnestesista.Criar(1, "Dr. Roberto Mendes", crm: "CRM/SP 89432",
            especialidade: "Anestesiologia geral");
        Assert.That(sut.Nome, Is.EqualTo("Dr. Roberto Mendes"));
        Assert.That(sut.Crm, Is.EqualTo("CRM/SP 89432"));
        Assert.That(sut.Ativo, Is.True);
        Assert.That(sut.Faixas, Is.Empty);
    }

    [Test]
    public void Criar_NomeVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => OrcamentoAnestesista.Criar(1, "  "));
    }

    [Test]
    public void Criar_EstabelecimentoInvalido_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => OrcamentoAnestesista.Criar(0, "Dr. Roberto"));
    }

    [Test]
    public void SincronizarFaixas_AdicionaFaixasNaOrdem()
    {
        var sut = OrcamentoAnestesista.Criar(1, "Dr. Roberto");
        sut.SincronizarFaixas(new[]
        {
            ("Pequeno porte", 1200m),
            ("Médio porte", 2200m),
            ("Grande porte", 3800m),
        });
        Assert.That(sut.Faixas.Count, Is.EqualTo(3));
        Assert.That(sut.Faixas.ElementAt(0).Descricao, Is.EqualTo("Pequeno porte"));
        Assert.That(sut.Faixas.ElementAt(0).Ordem, Is.EqualTo(0));
        Assert.That(sut.Faixas.ElementAt(2).Ordem, Is.EqualTo(2));
    }

    [Test]
    public void SincronizarFaixas_DuplicadasNaMesmaChamada_LancaBusinessException()
    {
        var sut = OrcamentoAnestesista.Criar(1, "Dr. Roberto");
        Assert.Throws<BusinessException>(() => sut.SincronizarFaixas(new[]
        {
            ("Pequeno porte", 1200m), ("Pequeno porte", 1300m),
        }));
    }

    [Test]
    public void SincronizarFaixas_SubstituiTotalmenteAsAnteriores()
    {
        var sut = OrcamentoAnestesista.Criar(1, "Dr. Roberto");
        sut.SincronizarFaixas(new[] { ("Faixa A", 100m), ("Faixa B", 200m) });
        sut.SincronizarFaixas(new[] { ("Faixa C", 300m) });
        Assert.That(sut.Faixas.Count, Is.EqualTo(1));
        Assert.That(sut.Faixas.Single().Descricao, Is.EqualTo("Faixa C"));
    }

    [Test]
    public void SincronizarFaixas_DescricaoVazia_LancaBusinessException()
    {
        var sut = OrcamentoAnestesista.Criar(1, "Dr. Roberto");
        Assert.Throws<BusinessException>(() => sut.SincronizarFaixas(new[] { ("", 100m) }));
    }
}
