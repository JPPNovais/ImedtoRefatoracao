using Imedto.Backend.Application.Estabelecimentos.Events;
using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.Domain.Financeiro;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Estabelecimentos;

/// <summary>
/// Testes do CriarSeedFinanceiroAoCriarEstabelecimentoHandler.
/// M2 (briefing 2026-06-22_003): seed lê do catálogo global (não mais hardcode).
/// CA3 — seed novo estabelecimento cria cópia de cada categoria ativa do global.
/// CA22 — FormasPagamento permanece hardcoded (intocado).
/// </summary>
[TestFixture]
public class CriarSeedFinanceiroAoCriarEstabelecimentoHandlerTests
{
    private Mock<ICategoriaFinanceiraPadraoSistemaRepository> _padraoRepo;
    private Mock<ICategoriaFinanceiraRepository> _categoriaRepo;
    private Mock<IFormaPagamentoRepository> _formaPagamentoRepo;
    private CriarSeedFinanceiroAoCriarEstabelecimentoHandler _sut;

    private const long EstabelecimentoId = 99;

    [SetUp]
    public void SetUp()
    {
        _padraoRepo = new Mock<ICategoriaFinanceiraPadraoSistemaRepository>();
        _categoriaRepo = new Mock<ICategoriaFinanceiraRepository>();
        _formaPagamentoRepo = new Mock<IFormaPagamentoRepository>();
        _sut = new CriarSeedFinanceiroAoCriarEstabelecimentoHandler(
            _padraoRepo.Object,
            _categoriaRepo.Object,
            _formaPagamentoRepo.Object);
    }

    private static CategoriaFinanceiraPadraoSistema GlobalAtiva(string nome, TipoCategoria tipo)
        => CategoriaFinanceiraPadraoSistema.Criar(nome, tipo);

    [Test]
    public async Task Handle_SeedNovEstabelecimento_CriaCopiaDeCadaGlobalAtiva_CA3()
    {
        // Arrange: 2 categorias ativas no global
        var globais = new List<CategoriaFinanceiraPadraoSistema>
        {
            GlobalAtiva("Consultas", TipoCategoria.Receita),
            GlobalAtiva("Aluguel", TipoCategoria.Despesa),
        };
        _padraoRepo.Setup(r => r.ListarAtivas(default)).ReturnsAsync(globais);

        var categoriasGravadas = new List<CategoriaFinanceira>();
        _categoriaRepo.Setup(r => r.Salvar(It.IsAny<CategoriaFinanceira>()))
            .Callback<CategoriaFinanceira>(c => categoriasGravadas.Add(c))
            .Returns(Task.CompletedTask);
        _formaPagamentoRepo.Setup(r => r.Salvar(It.IsAny<FormaPagamento>())).Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(new EstabelecimentoCriadoEvent(EstabelecimentoId, Guid.NewGuid(), "Clínica Teste"));

        // Assert
        Assert.That(categoriasGravadas, Has.Count.EqualTo(2));
        Assert.That(categoriasGravadas.All(c => c.EstabelecimentoId == EstabelecimentoId), Is.True);
        Assert.That(categoriasGravadas.All(c => c.Padrao), Is.True);
        Assert.That(categoriasGravadas.All(c => c.Ativo), Is.True);

        var nomes = categoriasGravadas.Select(c => c.Nome).ToList();
        Assert.That(nomes, Has.Member("Consultas"));
        Assert.That(nomes, Has.Member("Aluguel"));
    }

    [Test]
    public async Task Handle_GlobalVazia_NenhumaCategoriaGravada()
    {
        // Arrange: catálogo global vazio (ex.: ambiente de teste sem seed)
        _padraoRepo.Setup(r => r.ListarAtivas(default))
            .ReturnsAsync(new List<CategoriaFinanceiraPadraoSistema>());
        _formaPagamentoRepo.Setup(r => r.Salvar(It.IsAny<FormaPagamento>())).Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(new EstabelecimentoCriadoEvent(EstabelecimentoId, Guid.NewGuid(), "Clínica Teste"));

        // Assert — nenhuma categoria gravada, sem exceção
        _categoriaRepo.Verify(r => r.Salvar(It.IsAny<CategoriaFinanceira>()), Times.Never);
    }

    [Test]
    public async Task Handle_FormasPagamentoIntocadas_CA22()
    {
        // Arrange
        _padraoRepo.Setup(r => r.ListarAtivas(default))
            .ReturnsAsync(new List<CategoriaFinanceiraPadraoSistema>());

        var formasGravadas = new List<FormaPagamento>();
        _formaPagamentoRepo.Setup(r => r.Salvar(It.IsAny<FormaPagamento>()))
            .Callback<FormaPagamento>(f => formasGravadas.Add(f))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(new EstabelecimentoCriadoEvent(EstabelecimentoId, Guid.NewGuid(), "Clínica Teste"));

        // Assert: as 6 formas hardcoded continuam sendo criadas
        Assert.That(formasGravadas, Has.Count.EqualTo(SeedsFinanceiro.FormasPagamento.Count));
        var nomes = formasGravadas.Select(f => f.Nome).ToList();
        Assert.That(nomes, Has.Member("PIX"));
        Assert.That(nomes, Has.Member("Dinheiro"));
        Assert.That(nomes, Has.Member("Cartão de Crédito"));
    }

    [Test]
    public async Task Handle_NomeSemPrefixa_CopiaNomeExatamenteDoGlobal_CA3()
    {
        // Garante que nomes limpos (sem "Receita: " prefixo) são criados.
        var globais = new List<CategoriaFinanceiraPadraoSistema>
        {
            GlobalAtiva("Cirurgias", TipoCategoria.Receita),
        };
        _padraoRepo.Setup(r => r.ListarAtivas(default)).ReturnsAsync(globais);

        CategoriaFinanceira? gravada = null;
        _categoriaRepo.Setup(r => r.Salvar(It.IsAny<CategoriaFinanceira>()))
            .Callback<CategoriaFinanceira>(c => gravada = c)
            .Returns(Task.CompletedTask);
        _formaPagamentoRepo.Setup(r => r.Salvar(It.IsAny<FormaPagamento>())).Returns(Task.CompletedTask);

        await _sut.Handle(new EstabelecimentoCriadoEvent(EstabelecimentoId, Guid.NewGuid(), "Clínica Teste"));

        Assert.That(gravada, Is.Not.Null);
        Assert.That(gravada!.Nome, Is.EqualTo("Cirurgias")); // sem prefixo "Receita: "
        Assert.That(gravada.Nome, Does.Not.Contain("Receita:"));
        Assert.That(gravada.Nome, Does.Not.Contain("Despesa:"));
    }
}
