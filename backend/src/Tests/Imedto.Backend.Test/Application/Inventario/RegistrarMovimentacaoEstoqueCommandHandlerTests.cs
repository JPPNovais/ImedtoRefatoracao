using Imedto.Backend.Application.Inventario.Commands;
using Imedto.Backend.Contracts.Inventario.Commands;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Inventario;

[TestFixture]
public class RegistrarMovimentacaoEstoqueCommandHandlerTests
{
    private Mock<IItemInventarioRepository> _repo;
    private Mock<IMovimentacaoEstoqueRepository> _movRepo;
    private Mock<IEventBus> _eventBus;
    private RegistrarMovimentacaoEstoqueCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ItemId = 50;
    private readonly Guid _usuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IItemInventarioRepository>();
        _movRepo = new Mock<IMovimentacaoEstoqueRepository>();
        _eventBus = new Mock<IEventBus>();
        _sut = new RegistrarMovimentacaoEstoqueCommandHandler(_repo.Object, _movRepo.Object, _eventBus.Object);
    }

    private static ItemInventario ItemComEstoque(long estabId)
    {
        var item = ItemInventario.Criar(estabId, "COD-1", "Item", "Cat", "un", 0m);
        // Estoque inicial via Entrada para permitir Saida posterior.
        item.RegistrarEntrada(10m, Guid.NewGuid(), 5m, "init");
        return item;
    }

    [Test]
    public async Task Handle_EntradaValida_RegistraMovimentacaoEAtualizaEstoque()
    {
        var item = ItemComEstoque(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId)).ReturnsAsync(item);

        await _sut.Handle(new RegistrarMovimentacaoEstoqueCommand
        {
            ItemInventarioId = ItemId,
            EstabelecimentoId = EstabelecimentoId,
            Tipo = "Entrada",
            Quantidade = 5m,
            CustoUnitario = 10m,
            UsuarioId = _usuarioId,
        });

        _repo.Verify(r => r.Salvar(item), Times.Once);
        _movRepo.Verify(r => r.Salvar(It.IsAny<MovimentacaoEstoque>()), Times.Once);
    }

    [Test]
    public async Task Handle_SaidaValida_RegistraMovimentacao()
    {
        var item = ItemComEstoque(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId)).ReturnsAsync(item);

        await _sut.Handle(new RegistrarMovimentacaoEstoqueCommand
        {
            ItemInventarioId = ItemId,
            EstabelecimentoId = EstabelecimentoId,
            Tipo = "Saida",
            Quantidade = 3m,
            UsuarioId = _usuarioId,
        });

        _movRepo.Verify(r => r.Salvar(It.IsAny<MovimentacaoEstoque>()), Times.Once);
    }

    [Test]
    public void Handle_EntradaSemCustoUnitario_LancaBusinessException()
    {
        var item = ItemComEstoque(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId)).ReturnsAsync(item);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RegistrarMovimentacaoEstoqueCommand
        {
            ItemInventarioId = ItemId,
            EstabelecimentoId = EstabelecimentoId,
            Tipo = "Entrada",
            Quantidade = 5m,
            CustoUnitario = 0m,
            UsuarioId = _usuarioId,
        }));
        Assert.That(ex.Message, Does.Contain("Custo"));
    }

    [Test]
    public void Handle_TipoInvalido_LancaBusinessException()
    {
        var item = ItemComEstoque(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId)).ReturnsAsync(item);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RegistrarMovimentacaoEstoqueCommand
        {
            ItemInventarioId = ItemId,
            EstabelecimentoId = EstabelecimentoId,
            Tipo = "Furto",
            Quantidade = 1m,
            UsuarioId = _usuarioId,
        }));
        Assert.That(ex.Message, Does.Contain("inválido"));
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ItemId)).ReturnsAsync(ItemComEstoque(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RegistrarMovimentacaoEstoqueCommand
        {
            ItemInventarioId = ItemId,
            EstabelecimentoId = EstabelecimentoId,
            Tipo = "Entrada",
            Quantidade = 1m,
            CustoUnitario = 1m,
            UsuarioId = _usuarioId,
        }));
        Assert.That(ex.Message, Is.EqualTo("Item não encontrado."));
        _movRepo.Verify(r => r.Salvar(It.IsAny<MovimentacaoEstoque>()), Times.Never);
    }
}
