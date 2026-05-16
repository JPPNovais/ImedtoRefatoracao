using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Inventario;

[TestFixture]
public class MovimentacaoEstoqueSoftDeleteTests
{
    private static MovimentacaoEstoque CriarMovimentacao()
    {
        var item = ItemInventario.Criar(
            estabelecimentoId: 1,
            codigo: "MED001",
            nome: "Seringa",
            categoriaId: 10,
            categoriaNomeSnapshot: "Material",
            unidadeMedida: "un",
            quantidadeMinima: 0,
            fabricanteId: null,
            fornecedorPadraoId: null,
            localPadraoId: null,
            custoUnitario: null);
        item.RegistrarEntrada(10, Guid.NewGuid(), 5m, null);
        return item.RegistrarSaida(2, Guid.NewGuid(), null);
    }

    [Test]
    public void MarcarComoDeletado_UsuarioValido_SetaDeletadoEmEDeletadoPorUsuarioId()
    {
        var mov = CriarMovimentacao();
        var usuario = Guid.NewGuid();
        var antes = DateTime.UtcNow;

        mov.MarcarComoDeletado(usuario);

        Assert.That(mov.DeletadoEm, Is.Not.Null);
        Assert.That(mov.DeletadoEm!.Value, Is.GreaterThanOrEqualTo(antes));
        Assert.That(mov.DeletadoPorUsuarioId, Is.EqualTo(usuario));
    }

    [Test]
    public void MarcarComoDeletado_UsuarioVazio_LancaBusinessException()
    {
        var mov = CriarMovimentacao();

        var ex = Assert.Throws<BusinessException>(() =>
            mov.MarcarComoDeletado(Guid.Empty));

        Assert.That(ex.Message, Does.Contain("obrigatório").IgnoreCase);
    }

    [Test]
    public void MarcarComoDeletado_ChamadaDuasVezes_LancaBusinessException()
    {
        var mov = CriarMovimentacao();
        mov.MarcarComoDeletado(Guid.NewGuid());

        var ex = Assert.Throws<BusinessException>(() =>
            mov.MarcarComoDeletado(Guid.NewGuid()));

        Assert.That(ex.Message, Does.Contain("deletada").IgnoreCase);
    }
}
