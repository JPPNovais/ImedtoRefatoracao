using Imedto.Backend.Application.Inventario.Commands;
using Imedto.Backend.Contracts.Inventario.Commands;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Inventario;

[TestFixture]
public class AtualizarItemInventarioCommandHandlerTests
{
    private Mock<IItemInventarioRepository> _repo;
    private Mock<ICategoriaEstoqueRepository> _catRepo;
    private Mock<IFabricanteEstoqueRepository> _fabRepo;
    private Mock<IFornecedorEstoqueRepository> _fornRepo;
    private Mock<ILocalEstoqueRepository> _localRepo;
    private AtualizarItemInventarioCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long ItemId = 50;
    private const long CategoriaId = 10;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IItemInventarioRepository>();
        _catRepo = new Mock<ICategoriaEstoqueRepository>();
        _fabRepo = new Mock<IFabricanteEstoqueRepository>();
        _fornRepo = new Mock<IFornecedorEstoqueRepository>();
        _localRepo = new Mock<ILocalEstoqueRepository>();

        var categoria = CategoriaEstoque.Criar(EstabelecimentoId, "Nova Cat", "hsl(218 70% 50%)", "fa-tag");
        typeof(CategoriaEstoque).GetProperty("Id")!.SetValue(categoria, CategoriaId);
        _catRepo.Setup(r => r.ObterPorIdOuNulo(CategoriaId, EstabelecimentoId)).ReturnsAsync(categoria);

        _sut = new AtualizarItemInventarioCommandHandler(
            _repo.Object, _catRepo.Object, _fabRepo.Object, _fornRepo.Object, _localRepo.Object);
    }

    private static ItemInventario ItemNoEstab(long estabId) =>
        ItemInventario.Criar(estabId, "COD-1", "Original",
            categoriaId: CategoriaId, categoriaNomeSnapshot: "Categoria",
            unidadeMedida: "un", quantidadeMinima: 0m,
            fabricanteId: null, fornecedorPadraoId: null, localPadraoId: null, custoUnitario: null);

    private static AtualizarItemInventarioCommand Cmd() => new()
    {
        ItemId = ItemId,
        EstabelecimentoId = EstabelecimentoId,
        Nome = "Atualizado",
        CategoriaId = CategoriaId,
        UnidadeMedida = "kg",
        QuantidadeMinima = 5m,
    };

    [Test]
    public async Task Handle_DoMesmoTenant_AtualizaCampos()
    {
        var item = ItemNoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId, EstabelecimentoId)).ReturnsAsync(item);

        await _sut.Handle(Cmd());

        Assert.That(item.Nome, Is.EqualTo("Atualizado"));
        Assert.That(item.QuantidadeMinima, Is.EqualTo(5m));
        _repo.Verify(r => r.Salvar(item), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId, EstabelecimentoId)).ReturnsAsync((ItemInventario?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Item não encontrado."));
        _repo.Verify(r => r.Salvar(It.IsAny<ItemInventario>()), Times.Never);
    }

    [Test]
    public void Handle_Inexistente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId, EstabelecimentoId)).ReturnsAsync((ItemInventario)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }
}
