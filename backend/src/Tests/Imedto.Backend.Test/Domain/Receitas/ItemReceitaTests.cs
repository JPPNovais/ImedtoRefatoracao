using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Receitas;

/// <summary>
/// ItemReceita só é construído via Receita.Emitir (fábrica interna). Testamos
/// os comportamentos via integração com o aggregate pai.
/// </summary>
[TestFixture]
public class ItemReceitaTests
{
    private static (string, string, string, ViaAdministracao, string) ItemComVia(
        string medicamento, string posologia, string quantidade, ViaAdministracao via) =>
        (medicamento, posologia, quantidade, via, null);

    [Test]
    public void Emitir_ItemComMedicamentoEPosologiaValidos_CriaItemCorretamente()
    {
        // Arrange — tuple com tipo explícito para ViaAdministracao? nullable
        var itens = new (string, string, string, ViaAdministracao?, string)[]
        {
            ("Amoxicilina 500mg", "1 cápsula a cada 8h por 7 dias", "21 cápsulas", ViaAdministracao.Oral, null)
        };

        var receita = Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null, itens);
        var item = receita.Itens[0];

        Assert.That(item.Medicamento, Is.EqualTo("Amoxicilina 500mg"));
        Assert.That(item.Posologia, Is.EqualTo("1 cápsula a cada 8h por 7 dias"));
        Assert.That(item.Quantidade, Is.EqualTo("21 cápsulas"));
        Assert.That(item.Via, Is.EqualTo(ViaAdministracao.Oral));
    }

    [Test]
    public void Emitir_ItemComMedicamentoVazio_LancaBusinessException()
    {
        var itens = new (string, string, string, ViaAdministracao?, string)[]
        {
            ("", "a cada 8h", null, null, null)
        };

        var ex = Assert.Throws<BusinessException>(() =>
            Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null, itens));

        Assert.That(ex.Message, Does.Contain("Medicamento é obrigatório"));
    }

    [Test]
    public void Emitir_ItemComPosologiaVazia_LancaBusinessException()
    {
        var itens = new (string, string, string, ViaAdministracao?, string)[]
        {
            ("Dipirona", "  ", null, null, null)
        };

        var ex = Assert.Throws<BusinessException>(() =>
            Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null, itens));

        Assert.That(ex.Message, Does.Contain("Posologia é obrigatória"));
    }

    [Test]
    public void Emitir_ItemComMedicamentoAcimaDoLimite_LancaBusinessException()
    {
        var medicamentoLongo = new string('A', 201);
        var itens = new (string, string, string, ViaAdministracao?, string)[]
        {
            (medicamentoLongo, "a cada 8h", null, null, null)
        };

        var ex = Assert.Throws<BusinessException>(() =>
            Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null, itens));

        Assert.That(ex.Message, Does.Contain("200 caracteres"));
    }

    [Test]
    public void Emitir_ItemSemVia_ViaFicaNula()
    {
        var itens = new (string, string, string, ViaAdministracao?, string)[]
        {
            ("Dipirona 500mg", "a cada 8h", null, null, null)
        };

        var receita = Receita.Emitir(1, 1, Guid.NewGuid(), 1, TipoReceita.Comum, null, null, itens);

        Assert.That(receita.Itens[0].Via, Is.Null);
    }
}
