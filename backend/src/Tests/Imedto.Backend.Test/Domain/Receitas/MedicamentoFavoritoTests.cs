using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Receitas;

[TestFixture]
public class MedicamentoFavoritoTests
{
    private static readonly Guid ProfissionalId = Guid.NewGuid();

    [Test]
    public void CriarOuIncrementar_PrimeiraVez_UsoCountIniciaEm1()
    {
        var favorito = MedicamentoFavorito.CriarOuIncrementar(
            ProfissionalId, 1, "Dipirona 500mg", "1 comprimido a cada 8h", ViaAdministracao.Oral);

        Assert.That(favorito.UsoCount, Is.EqualTo(1));
        Assert.That(favorito.UltimoUso, Is.Not.Null);
    }

    [Test]
    public void CriarOuIncrementar_PrimeiraVez_CamposPopuladosCorretamente()
    {
        var favorito = MedicamentoFavorito.CriarOuIncrementar(
            ProfissionalId, 1, "  Amoxicilina 500mg  ", "a cada 8h", null);

        Assert.That(favorito.Medicamento, Is.EqualTo("Amoxicilina 500mg")); // trim
        Assert.That(favorito.ProfissionalUsuarioId, Is.EqualTo(ProfissionalId));
        Assert.That(favorito.EstabelecimentoId, Is.EqualTo(1));
    }

    [Test]
    public void IncrementarUso_DeveIncrementarContadorEmUm()
    {
        var favorito = MedicamentoFavorito.CriarOuIncrementar(
            ProfissionalId, 1, "Dipirona 500mg", null, null);

        favorito.IncrementarUso();

        Assert.That(favorito.UsoCount, Is.EqualTo(2));
    }

    [Test]
    public void IncrementarUso_DeveAtualizarUltimoUso()
    {
        var favorito = MedicamentoFavorito.CriarOuIncrementar(
            ProfissionalId, 1, "Dipirona 500mg", null, null);
        var ultimoUsoAntes = favorito.UltimoUso;

        favorito.IncrementarUso();

        Assert.That(favorito.UltimoUso, Is.GreaterThanOrEqualTo(ultimoUsoAntes));
    }

    [Test]
    public void IncrementarUso_MultiplasChamadas_ContadorAcumula()
    {
        var favorito = MedicamentoFavorito.CriarOuIncrementar(
            ProfissionalId, 1, "Dipirona 500mg", null, null);

        favorito.IncrementarUso();
        favorito.IncrementarUso();
        favorito.IncrementarUso();

        Assert.That(favorito.UsoCount, Is.EqualTo(4)); // 1 inicial + 3
    }

    [Test]
    public void CriarOuIncrementar_MedicamentoVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            MedicamentoFavorito.CriarOuIncrementar(ProfissionalId, 1, "  ", null, null));

        Assert.That(ex.Message, Does.Contain("Medicamento é obrigatório"));
    }

    [Test]
    public void CriarOuIncrementar_ProfissionalVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            MedicamentoFavorito.CriarOuIncrementar(Guid.Empty, 1, "Dipirona", null, null));

        Assert.That(ex.Message, Does.Contain("Profissional é obrigatório"));
    }

    [Test]
    public void CriarOuIncrementar_EstabelecimentoZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            MedicamentoFavorito.CriarOuIncrementar(ProfissionalId, 0, "Dipirona", null, null));

        Assert.That(ex.Message, Does.Contain("Estabelecimento é obrigatório"));
    }
}
