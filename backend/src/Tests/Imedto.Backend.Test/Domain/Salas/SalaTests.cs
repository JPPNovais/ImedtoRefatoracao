using Imedto.Backend.Domain.Salas;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Salas;

[TestFixture]
public class SalaTests
{
    private static Sala CriarValida() =>
        Sala.Criar(
            estabelecimentoId: 1L,
            unidadeId: 10L,
            tipoSalaId: 5L,
            nome: " Consultorio 1 ",
            descricao: " Sala azul ");

    [Test]
    public void Criar_Valida_Defaults()
    {
        var s = CriarValida();
        Assert.That(s.EstabelecimentoId, Is.EqualTo(1L));
        Assert.That(s.UnidadeId, Is.EqualTo(10L));
        Assert.That(s.TipoSalaId, Is.EqualTo(5L));
        Assert.That(s.Nome, Is.EqualTo("Consultorio 1"));
        Assert.That(s.Descricao, Is.EqualTo("Sala azul"));
        Assert.That(s.Ativo, Is.True);
        Assert.That(s.CriadoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void Criar_DescricaoVazia_PermiteNull()
    {
        var s = Sala.Criar(1L, 10L, null, "Sala", "  ");
        Assert.That(s.Descricao, Is.Null);
        Assert.That(s.TipoSalaId, Is.Null);
    }

    [Test]
    public void Criar_EstabelecimentoZero_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => Sala.Criar(0L, 10L, null, "Sala", null));
    }

    [Test]
    public void Criar_UnidadeZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() => Sala.Criar(1L, 0L, null, "Sala", null));
        Assert.That(ex.Message, Does.Contain("unidade"));
    }

    [Test]
    public void Criar_NomeVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() => Sala.Criar(1L, 10L, null, " ", null));
        Assert.That(ex.Message, Does.Contain("Nome"));
    }

    [Test]
    public void AtualizarDados_Valido_AtualizaCampos()
    {
        var s = CriarValida();
        s.AtualizarDados(20L, 7L, "Sala B", "Nova");

        Assert.That(s.UnidadeId, Is.EqualTo(20L));
        Assert.That(s.TipoSalaId, Is.EqualTo(7L));
        Assert.That(s.Nome, Is.EqualTo("Sala B"));
        Assert.That(s.Descricao, Is.EqualTo("Nova"));
        Assert.That(s.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void AtualizarDados_UnidadeZero_LancaBusinessException()
    {
        var s = CriarValida();
        Assert.Throws<BusinessException>(() => s.AtualizarDados(0L, null, "X", null));
    }

    [Test]
    public void AtualizarDados_NomeVazio_LancaBusinessException()
    {
        var s = CriarValida();
        Assert.Throws<BusinessException>(() => s.AtualizarDados(10L, null, " ", null));
    }
}
