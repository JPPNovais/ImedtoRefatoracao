using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Inventario;

[TestFixture]
public class ItemInventarioTests
{
    private static ItemInventario CriarItem() =>
        ItemInventario.Criar(
            estabelecimentoId: 1,
            codigo: "MED001",
            nome: "Seringa",
            categoria: "Material",
            unidadeMedida: "un",
            quantidadeMinima: 5);

    // --- CustoMedio ---

    [Test]
    public void RegistrarEntrada_EstoqueVazio_CustoMedioIgualAoCustoUnitario()
    {
        var item = CriarItem();

        item.RegistrarEntrada(quantidade: 10, usuarioId: Guid.NewGuid(), custoUnitario: 5m, observacao: null);

        Assert.That(item.CustoMedio, Is.EqualTo(5m));
    }

    [Test]
    public void RegistrarEntrada_DuasEntradas_CustoMedioPonderadoCorreto()
    {
        // 10 un × R$5 + 10 un × R$7 → CustoMedio = R$6
        var item = CriarItem();
        item.RegistrarEntrada(10, Guid.NewGuid(), 5m, null);

        item.RegistrarEntrada(10, Guid.NewGuid(), 7m, null);

        Assert.That(item.CustoMedio, Is.EqualTo(6m));
    }

    [Test]
    public void RegistrarSaida_UsaCustoMedioComoSnapshot_CustoUnitarioDaMovimentacao()
    {
        // Após 10@5 + 10@7 (CustoMedio=6), saída de 5 un → CustoUnitario na movimentação = 6
        var item = CriarItem();
        item.RegistrarEntrada(10, Guid.NewGuid(), 5m, null);
        item.RegistrarEntrada(10, Guid.NewGuid(), 7m, null);

        var mov = item.RegistrarSaida(quantidade: 5, usuarioId: Guid.NewGuid(), observacao: null);

        Assert.That(mov.CustoUnitario, Is.EqualTo(6m));
        Assert.That(mov.CustoTotal, Is.EqualTo(30m));
    }

    [Test]
    public void RegistrarSaida_NaoAlteraCustoMedioDoItem()
    {
        var item = CriarItem();
        item.RegistrarEntrada(10, Guid.NewGuid(), 5m, null);
        item.RegistrarEntrada(10, Guid.NewGuid(), 7m, null);
        var custoMedioAntes = item.CustoMedio;

        item.RegistrarSaida(5, Guid.NewGuid(), null);

        Assert.That(item.CustoMedio, Is.EqualTo(custoMedioAntes));
    }

    [Test]
    public void RegistrarEntrada_CustoUnitarioNegativo_LancaBusinessException()
    {
        var item = CriarItem();

        var ex = Assert.Throws<BusinessException>(() =>
            item.RegistrarEntrada(10, Guid.NewGuid(), custoUnitario: -1m, null));

        Assert.That(ex.Message, Does.Contain("negativo").IgnoreCase);
    }

    [Test]
    public void RegistrarEntrada_CustoUnitarioZero_NaoLancaExcecao()
    {
        // custoUnitario == 0 é permitido (item sem custo registrado — validação é >= 0)
        var item = CriarItem();

        Assert.DoesNotThrow(() =>
            item.RegistrarEntrada(10, Guid.NewGuid(), custoUnitario: 0m, null));
    }

    [Test]
    public void RegistrarSaida_QuantidadeAcimaDoEstoque_LancaBusinessException()
    {
        var item = CriarItem();
        item.RegistrarEntrada(5, Guid.NewGuid(), 10m, null);

        var ex = Assert.Throws<BusinessException>(() =>
            item.RegistrarSaida(quantidade: 6, Guid.NewGuid(), null));

        Assert.That(ex.Message, Does.Contain("insuficiente").IgnoreCase);
    }

    [Test]
    public void RegistrarEntrada_RetornaMovimentacaoComCamposCorretos()
    {
        var item = CriarItem();
        var usuario = Guid.NewGuid();

        var mov = item.RegistrarEntrada(8, usuario, 12m, "Nota fiscal 123");

        Assert.That(mov.Quantidade, Is.EqualTo(8m));
        Assert.That(mov.CustoUnitario, Is.EqualTo(12m));
        Assert.That(mov.CustoTotal, Is.EqualTo(96m));
        Assert.That(mov.CriadoPorUsuarioId, Is.EqualTo(usuario));
        Assert.That(mov.Tipo, Is.EqualTo(TipoMovimentacaoEstoque.Entrada));
    }
}
