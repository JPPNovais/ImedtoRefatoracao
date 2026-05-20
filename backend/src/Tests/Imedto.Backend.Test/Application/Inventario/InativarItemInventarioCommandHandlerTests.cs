using Imedto.Backend.Application.Inventario.Commands;
using Imedto.Backend.Contracts.Inventario.Commands;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Inventario;

[TestFixture]
public class InativarItemInventarioCommandHandlerTests
{
    private Mock<IItemInventarioRepository> _repo;
    private Mock<IMovimentacaoEstoqueRepository> _movRepo;
    private InativarItemInventarioCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ItemId = 50;
    private static readonly Guid UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IItemInventarioRepository>();
        _movRepo = new Mock<IMovimentacaoEstoqueRepository>();
        _sut = new InativarItemInventarioCommandHandler(_repo.Object, _movRepo.Object);
    }

    private static ItemInventario ItemNoEstab(long estabId) =>
        ItemInventario.Criar(estabId, "COD-1", "Item",
            categoriaId: 10, categoriaNomeSnapshot: "Cat",
            unidadeMedida: "un", quantidadeMinima: 0m,
            fabricanteId: null, fornecedorPadraoId: null, localPadraoId: null, custoUnitario: null);

    [Test]
    public async Task Handle_DoMesmoTenant_Inativa()
    {
        var item = ItemNoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId, EstabelecimentoId)).ReturnsAsync(item);

        await _sut.Handle(new InativarItemInventarioCommand
        {
            ItemId = ItemId,
            EstabelecimentoId = EstabelecimentoId,
            UsuarioId = UsuarioId,
        });

        Assert.That(item.Ativo, Is.False);
        _repo.Verify(r => r.Salvar(item), Times.Once);
    }

    [Test]
    public async Task Handle_DoMesmoTenant_RegistraMovimentacaoDeInativacao()
    {
        var item = ItemNoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId, EstabelecimentoId)).ReturnsAsync(item);

        await _sut.Handle(new InativarItemInventarioCommand
        {
            ItemId = ItemId,
            EstabelecimentoId = EstabelecimentoId,
            UsuarioId = UsuarioId,
            Observacao = "saiu da linha",
        });

        _movRepo.Verify(r => r.Salvar(It.Is<MovimentacaoEstoque>(m =>
            m.Tipo == TipoMovimentacaoEstoque.Inativacao
            && m.CriadoPorUsuarioId == UsuarioId
            && m.Observacao == "saiu da linha"
            && m.Quantidade == 0m)), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId, EstabelecimentoId)).ReturnsAsync((ItemInventario?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new InativarItemInventarioCommand
        {
            ItemId = ItemId,
            EstabelecimentoId = EstabelecimentoId,
            UsuarioId = UsuarioId,
        }));
        Assert.That(ex.Message, Is.EqualTo("Item não encontrado."));
        _repo.Verify(r => r.Salvar(It.IsAny<ItemInventario>()), Times.Never);
        _movRepo.Verify(r => r.Salvar(It.IsAny<MovimentacaoEstoque>()), Times.Never);
    }
}
