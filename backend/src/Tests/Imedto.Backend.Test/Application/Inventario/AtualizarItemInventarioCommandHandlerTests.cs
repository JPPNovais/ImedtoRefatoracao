using Imedto.Backend.Application.Inventario.Commands;
using Imedto.Backend.Contracts.Inventario.Commands;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Inventario;

[TestFixture]
public class AtualizarItemInventarioCommandHandlerTests
{
    private Mock<IItemInventarioRepository> _repo;
    private AtualizarItemInventarioCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ItemId = 50;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IItemInventarioRepository>();
        _sut = new AtualizarItemInventarioCommandHandler(_repo.Object);
    }

    private static ItemInventario ItemNoEstab(long estabId) =>
        ItemInventario.Criar(estabId, "COD-1", "Original", "Categoria", "un", 0m);

    private static AtualizarItemInventarioCommand Cmd() => new()
    {
        ItemId = ItemId,
        EstabelecimentoId = EstabelecimentoId,
        Nome = "Atualizado",
        Categoria = "Nova Cat",
        UnidadeMedida = "kg",
        QuantidadeMinima = 5m,
    };

    [Test]
    public async Task Handle_DoMesmoTenant_AtualizaCampos()
    {
        var item = ItemNoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId)).ReturnsAsync(item);

        await _sut.Handle(Cmd());

        Assert.That(item.Nome, Is.EqualTo("Atualizado"));
        Assert.That(item.QuantidadeMinima, Is.EqualTo(5m));
        _repo.Verify(r => r.Salvar(item), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId)).ReturnsAsync(ItemNoEstab(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Item não encontrado."));
        _repo.Verify(r => r.Salvar(It.IsAny<ItemInventario>()), Times.Never);
    }

    [Test]
    public void Handle_Inexistente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId)).ReturnsAsync((ItemInventario)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }
}
