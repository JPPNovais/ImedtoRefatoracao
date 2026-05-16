using Imedto.Backend.Application.Inventario.Commands;
using Imedto.Backend.Contracts.Inventario.Commands;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Inventario;

[TestFixture]
public class CriarItemInventarioCommandHandlerTests
{
    private Mock<IItemInventarioRepository> _repo;
    private Mock<IMovimentacaoEstoqueRepository> _movRepo;
    private Mock<ICategoriaEstoqueRepository> _catRepo;
    private Mock<IFabricanteEstoqueRepository> _fabRepo;
    private Mock<IFornecedorEstoqueRepository> _fornRepo;
    private Mock<ILocalEstoqueRepository> _localRepo;
    private Mock<IEventBus> _eventBus;
    private CriarItemInventarioCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long CategoriaId = 10;
    private readonly Guid _criadoPorId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IItemInventarioRepository>();
        _movRepo = new Mock<IMovimentacaoEstoqueRepository>();
        _catRepo = new Mock<ICategoriaEstoqueRepository>();
        _fabRepo = new Mock<IFabricanteEstoqueRepository>();
        _fornRepo = new Mock<IFornecedorEstoqueRepository>();
        _localRepo = new Mock<ILocalEstoqueRepository>();
        _eventBus = new Mock<IEventBus>();

        // Categoria default existe e está ativa. Setamos o Id via reflection — em prod
        // o EF popula via SaveChanges; aqui o mock precisa ter um Id > 0 para o
        // aggregate ItemInventario.Criar não falhar a guarda de categoriaId.
        var categoria = CategoriaEstoque.Criar(EstabelecimentoId, "Materiais", "hsl(218 70% 50%)", "fa-tag");
        typeof(CategoriaEstoque).GetProperty("Id")!.SetValue(categoria, CategoriaId);
        _catRepo.Setup(r => r.ObterPorIdOuNulo(CategoriaId, EstabelecimentoId)).ReturnsAsync(categoria);

        _sut = new CriarItemInventarioCommandHandler(
            _repo.Object, _movRepo.Object,
            _catRepo.Object, _fabRepo.Object, _fornRepo.Object, _localRepo.Object,
            _eventBus.Object);
    }

    private CriarItemInventarioCommand Cmd(decimal qtdInicial = 0, decimal custoInicial = 0) => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        Codigo = "COD-1",
        Nome = "Item",
        CategoriaId = CategoriaId,
        UnidadeMedida = "un",
        QuantidadeInicial = qtdInicial,
        QuantidadeMinima = 0m,
        CustoUnitarioInicial = custoInicial,
        CriadoPorUsuarioId = _criadoPorId,
    };

    [Test]
    public async Task Handle_SemEstoqueInicial_PersisteApenasItem()
    {
        await _sut.Handle(Cmd());

        _repo.Verify(r => r.Salvar(It.IsAny<ItemInventario>()), Times.Once);
        _movRepo.Verify(r => r.Salvar(It.IsAny<MovimentacaoEstoque>()), Times.Never,
            "Sem estoque inicial → não deve gerar movimentação.");
    }

    [Test]
    public async Task Handle_ComEstoqueInicial_PersisteItemEMovimentacao()
    {
        await _sut.Handle(Cmd(qtdInicial: 10m, custoInicial: 5m));

        _repo.Verify(r => r.Salvar(It.IsAny<ItemInventario>()), Times.Exactly(2),
            "Salvar 1x antes da Entrada (popular Id) + 1x após registrar movimentação.");
        _movRepo.Verify(r => r.Salvar(It.IsAny<MovimentacaoEstoque>()), Times.Once);
    }

    [Test]
    public void Handle_EstoqueInicialSemCusto_LancaBusinessException()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.Handle(Cmd(qtdInicial: 10m, custoInicial: 0m)));
        Assert.That(ex.Message, Does.Contain("Custo"));
        _movRepo.Verify(r => r.Salvar(It.IsAny<MovimentacaoEstoque>()), Times.Never);
    }

    [Test]
    public void Handle_CategoriaInexistente_LancaBusinessException()
    {
        var cmd = Cmd();
        cmd.CategoriaId = 9999;
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("Categoria"));
        _repo.Verify(r => r.Salvar(It.IsAny<ItemInventario>()), Times.Never);
    }
}
