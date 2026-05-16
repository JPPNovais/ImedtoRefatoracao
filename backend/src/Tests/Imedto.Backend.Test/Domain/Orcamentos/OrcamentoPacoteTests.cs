using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Orcamentos;

[TestFixture]
public class OrcamentoPacoteTests
{
    [Test]
    public void Criar_ComNome_DefineCamposBasicosEAtiva()
    {
        var sut = OrcamentoPacote.Criar(1, "Pacote bariátrica", "Cirurgia + estrutura", null, 12000m);
        Assert.That(sut.Nome, Is.EqualTo("Pacote bariátrica"));
        Assert.That(sut.ValorTotalSugerido, Is.EqualTo(12000m));
        Assert.That(sut.Ativo, Is.True);
        Assert.That(sut.Procedimentos, Is.Empty);
    }

    [Test]
    public void Criar_NomeVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => OrcamentoPacote.Criar(1, "  ", null, null, null));
    }

    [Test]
    public void Criar_ValorNegativo_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => OrcamentoPacote.Criar(1, "Pacote X", null, null, -1m));
    }

    [Test]
    public void Sincronizar_PopulaAsTresColecoes()
    {
        var sut = OrcamentoPacote.Criar(1, "Pacote teste", null, null, null);
        sut.Sincronizar(
            procedimentoIds: new long[] { 10, 20 },
            produtos: new[] { (5L, 2m), (6L, 1m) },
            teamRoleIds: new long[] { 100, 200, 300 });
        Assert.That(sut.Procedimentos.Count, Is.EqualTo(2));
        Assert.That(sut.Produtos.Count, Is.EqualTo(2));
        Assert.That(sut.TeamRoles.Count, Is.EqualTo(3));
        Assert.That(sut.Procedimentos.First().Ordem, Is.EqualTo(0));
    }

    [Test]
    public void Sincronizar_ProcedimentoDuplicado_LancaBusinessException()
    {
        var sut = OrcamentoPacote.Criar(1, "Pacote teste", null, null, null);
        Assert.Throws<BusinessException>(() =>
            sut.Sincronizar(new long[] { 10, 10 }, Array.Empty<(long, decimal)>(), Array.Empty<long>()));
    }

    [Test]
    public void Sincronizar_ProdutoDuplicado_LancaBusinessException()
    {
        var sut = OrcamentoPacote.Criar(1, "Pacote teste", null, null, null);
        Assert.Throws<BusinessException>(() =>
            sut.Sincronizar(Array.Empty<long>(), new[] { (5L, 1m), (5L, 2m) }, Array.Empty<long>()));
    }

    [Test]
    public void Sincronizar_TeamRoleDuplicado_LancaBusinessException()
    {
        var sut = OrcamentoPacote.Criar(1, "Pacote teste", null, null, null);
        Assert.Throws<BusinessException>(() =>
            sut.Sincronizar(Array.Empty<long>(), Array.Empty<(long, decimal)>(), new long[] { 100, 100 }));
    }

    [Test]
    public void Sincronizar_SegundaChamadaSubstituiAsAssociacoes()
    {
        var sut = OrcamentoPacote.Criar(1, "Pacote teste", null, null, null);
        sut.Sincronizar(new long[] { 10 }, new[] { (5L, 1m) }, new long[] { 100 });
        sut.Sincronizar(new long[] { 20, 30 }, Array.Empty<(long, decimal)>(), new long[] { 200 });
        Assert.That(sut.Procedimentos.Select(p => p.CatalogoCirurgiaId), Is.EquivalentTo(new long[] { 20, 30 }));
        Assert.That(sut.Produtos, Is.Empty);
        Assert.That(sut.TeamRoles.Single().TeamRoleId, Is.EqualTo(200L));
    }

    [Test]
    public void Sincronizar_QuantidadeZero_LancaBusinessException()
    {
        var sut = OrcamentoPacote.Criar(1, "Pacote teste", null, null, null);
        Assert.Throws<BusinessException>(() =>
            sut.Sincronizar(Array.Empty<long>(), new[] { (5L, 0m) }, Array.Empty<long>()));
    }
}
