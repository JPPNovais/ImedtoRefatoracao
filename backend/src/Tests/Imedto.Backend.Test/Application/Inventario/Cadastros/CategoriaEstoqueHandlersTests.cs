using Imedto.Backend.Application.Inventario.Cadastros.Commands;
using Imedto.Backend.Contracts.Inventario.Cadastros.Commands;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Inventario.Cadastros;

[TestFixture]
public class CategoriaEstoqueHandlersTests
{
    private const long EstabA = 1;
    private const long EstabB = 2;
    private const string CorOk = "hsl(218 70% 50%)";
    private const string IconeOk = "fa-tag";

    private Mock<ICategoriaEstoqueRepository> _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ICategoriaEstoqueRepository>();
    }

    [Test]
    public async Task Criar_ComNomeNovo_PersisteCategoria()
    {
        _repo.Setup(r => r.ExisteComNomeNoEstabelecimento("Anestésicos", EstabA, null)).ReturnsAsync(false);
        var sut = new CriarCategoriaEstoqueCommandHandler(_repo.Object);
        var cmd = new CriarCategoriaEstoqueCommand { EstabelecimentoId = EstabA, Nome = "Anestésicos", Cor = CorOk, Icone = IconeOk };

        await sut.Handle(cmd);

        _repo.Verify(r => r.Salvar(It.IsAny<CategoriaEstoque>()), Times.Once);
    }

    [Test]
    public void Criar_NomeDuplicadoNoMesmoTenant_LancaBusinessException()
    {
        _repo.Setup(r => r.ExisteComNomeNoEstabelecimento("X", EstabA, null)).ReturnsAsync(true);
        var sut = new CriarCategoriaEstoqueCommandHandler(_repo.Object);

        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new CriarCategoriaEstoqueCommand
        {
            EstabelecimentoId = EstabA, Nome = "X", Cor = CorOk, Icone = IconeOk,
        }));
        Assert.That(ex.Message, Does.Contain("categoria"));
        _repo.Verify(r => r.Salvar(It.IsAny<CategoriaEstoque>()), Times.Never);
    }

    [Test]
    public async Task Criar_MesmoNomeEmOutroEstabelecimento_PermiteIsolamento()
    {
        // Multi-tenant: ExisteComNomeNoEstabelecimento(estab=B) retorna false porque o repo
        // filtra por tenant — handler permite criar.
        _repo.Setup(r => r.ExisteComNomeNoEstabelecimento("X", EstabB, null)).ReturnsAsync(false);
        var sut = new CriarCategoriaEstoqueCommandHandler(_repo.Object);

        await sut.Handle(new CriarCategoriaEstoqueCommand
        {
            EstabelecimentoId = EstabB, Nome = "X", Cor = CorOk, Icone = IconeOk,
        });

        _repo.Verify(r => r.Salvar(It.IsAny<CategoriaEstoque>()), Times.Once);
    }

    [Test]
    public void Atualizar_DeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(99, EstabA)).ReturnsAsync((CategoriaEstoque?)null);
        var sut = new AtualizarCategoriaEstoqueCommandHandler(_repo.Object);

        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new AtualizarCategoriaEstoqueCommand
        {
            CategoriaId = 99, EstabelecimentoId = EstabA, Nome = "Y", Cor = CorOk, Icone = IconeOk,
        }));
        Assert.That(ex.Message, Is.EqualTo("Categoria não encontrada."));
        _repo.Verify(r => r.Salvar(It.IsAny<CategoriaEstoque>()), Times.Never);
    }

    [Test]
    public void Inativar_ComItensVinculados_LancaBusinessException()
    {
        var cat = CategoriaEstoque.Criar(EstabA, "X", CorOk, IconeOk);
        _repo.Setup(r => r.ObterPorIdOuNulo(10, EstabA)).ReturnsAsync(cat);
        _repo.Setup(r => r.ExistemItensVinculados(10, EstabA)).ReturnsAsync(true);
        var sut = new InativarCategoriaEstoqueCommandHandler(_repo.Object);

        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new InativarCategoriaEstoqueCommand
        {
            CategoriaId = 10, EstabelecimentoId = EstabA,
        }));
        Assert.That(ex.Message, Does.Contain("itens"));
        _repo.Verify(r => r.Salvar(It.IsAny<CategoriaEstoque>()), Times.Never);
    }

    [Test]
    public async Task Inativar_SemItensVinculados_Inativa()
    {
        var cat = CategoriaEstoque.Criar(EstabA, "X", CorOk, IconeOk);
        _repo.Setup(r => r.ObterPorIdOuNulo(10, EstabA)).ReturnsAsync(cat);
        _repo.Setup(r => r.ExistemItensVinculados(10, EstabA)).ReturnsAsync(false);
        var sut = new InativarCategoriaEstoqueCommandHandler(_repo.Object);

        await sut.Handle(new InativarCategoriaEstoqueCommand
        {
            CategoriaId = 10, EstabelecimentoId = EstabA,
        });

        Assert.That(cat.Ativo, Is.False);
        _repo.Verify(r => r.Salvar(cat), Times.Once);
    }
}
