using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

/// <summary>
/// Testes dos handlers de Criar/Editar/Excluir modelos de descrição cirúrgica.
/// Multi-tenant falha-fechada, dedup de título e imutabilidade padrão-sistema.
/// </summary>
[TestFixture]
public class ModeloDescricaoCirurgicaCommandHandlerTests
{
    private Mock<IModeloDescricaoCirurgicaRepository> _repo;

    private const long EstabA = 1;
    private const long EstabB = 2;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IModeloDescricaoCirurgicaRepository>();
    }

    // ─── Criar ───────────────────────────────────────────────────────────────────

    [Test]
    public async Task Criar_SemConflito_PersisteDados()
    {
        _repo.Setup(r => r.ExisteOutroComMesmoTitulo(EstabA, "Rinoplastia", 0)).ReturnsAsync(false);

        var sut = new CriarModeloDescricaoCirurgicaCommandHandler(_repo.Object);
        await sut.Handle(new CriarModeloDescricaoCirurgicaCommand
        {
            EstabelecimentoId = EstabA,
            Titulo = "Rinoplastia",
            Corpo = "Técnica aberta..."
        });

        _repo.Verify(r => r.Salvar(It.Is<ModeloDescricaoCirurgica>(m =>
            m.Titulo == "Rinoplastia" && m.EstabelecimentoId == EstabA)),
            Times.Once);
    }

    [Test]
    public void Criar_TituloDuplicado_LancaBusinessException()
    {
        _repo.Setup(r => r.ExisteOutroComMesmoTitulo(EstabA, "Rinoplastia", 0)).ReturnsAsync(true);

        var sut = new CriarModeloDescricaoCirurgicaCommandHandler(_repo.Object);
        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new CriarModeloDescricaoCirurgicaCommand
        {
            EstabelecimentoId = EstabA,
            Titulo = "Rinoplastia",
            Corpo = "texto"
        }));
        Assert.That(ex.Message, Does.Contain("Já existe um modelo"));
    }

    // ─── Editar ───────────────────────────────────────────────────────────────────

    [Test]
    public async Task Editar_ModeloDoTenant_AtualizaDados()
    {
        var modelo = ModeloDescricaoCirurgica.CriarDoEstabelecimento(EstabA, "Rinoplastia", "original");
        _repo.Setup(r => r.ObterPorIdOuNulo(10, EstabA)).ReturnsAsync(modelo);
        _repo.Setup(r => r.ExisteOutroComMesmoTitulo(EstabA, "Rinoplastia Estruturada", 0)).ReturnsAsync(false);

        var sut = new EditarModeloDescricaoCirurgicaCommandHandler(_repo.Object);
        await sut.Handle(new EditarModeloDescricaoCirurgicaCommand
        {
            ModeloId = 10,
            EstabelecimentoId = EstabA,
            Titulo = "Rinoplastia Estruturada",
            Corpo = "novo corpo"
        });

        _repo.Verify(r => r.Salvar(It.IsAny<ModeloDescricaoCirurgica>()), Times.Once);
    }

    [Test]
    public void Editar_ModeloNaoEncontrado_LancaBusinessException()
    {
        // CA6: id do estabelecimento B tentando acessar modelo do estabelecimento A → not found
        _repo.Setup(r => r.ObterPorIdOuNulo(99, EstabB)).ReturnsAsync((ModeloDescricaoCirurgica?)null);

        var sut = new EditarModeloDescricaoCirurgicaCommandHandler(_repo.Object);
        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new EditarModeloDescricaoCirurgicaCommand
        {
            ModeloId = 99,
            EstabelecimentoId = EstabB,
            Titulo = "X",
            Corpo = "Y"
        }));
        Assert.That(ex.Message, Does.Contain("não encontrado").IgnoreCase);
    }

    [Test]
    public void Editar_TituloDuplicado_LancaBusinessException()
    {
        var modelo = ModeloDescricaoCirurgica.CriarDoEstabelecimento(EstabA, "Rinoplastia", "original");
        _repo.Setup(r => r.ObterPorIdOuNulo(10, EstabA)).ReturnsAsync(modelo);
        _repo.Setup(r => r.ExisteOutroComMesmoTitulo(EstabA, "Colecistectomia", 0)).ReturnsAsync(true);

        var sut = new EditarModeloDescricaoCirurgicaCommandHandler(_repo.Object);
        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new EditarModeloDescricaoCirurgicaCommand
        {
            ModeloId = 10,
            EstabelecimentoId = EstabA,
            Titulo = "Colecistectomia",
            Corpo = "texto"
        }));
        Assert.That(ex.Message, Does.Contain("Já existe um modelo"));
    }

    // ─── Excluir ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Excluir_ModeloDoTenant_RemoveDados()
    {
        var modelo = ModeloDescricaoCirurgica.CriarDoEstabelecimento(EstabA, "Rinoplastia", "texto");
        _repo.Setup(r => r.ObterPorIdOuNulo(10, EstabA)).ReturnsAsync(modelo);

        var sut = new ExcluirModeloDescricaoCirurgicaCommandHandler(_repo.Object);
        await sut.Handle(new ExcluirModeloDescricaoCirurgicaCommand
        {
            ModeloId = 10,
            EstabelecimentoId = EstabA
        });

        _repo.Verify(r => r.Excluir(modelo), Times.Once);
    }

    [Test]
    public void Excluir_ModeloNaoEncontrado_LancaBusinessException()
    {
        // CA6: tenant B tenta excluir modelo do tenant A → not found (mensagem genérica)
        _repo.Setup(r => r.ObterPorIdOuNulo(99, EstabB)).ReturnsAsync((ModeloDescricaoCirurgica?)null);

        var sut = new ExcluirModeloDescricaoCirurgicaCommandHandler(_repo.Object);
        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new ExcluirModeloDescricaoCirurgicaCommand
        {
            ModeloId = 99,
            EstabelecimentoId = EstabB
        }));
        // CA12: mensagem genérica — não revela tenant alheio
        Assert.That(ex.Message, Does.Contain("não encontrado").IgnoreCase);
        Assert.That(ex.Message, Does.Not.Contain("tenant"));
    }

    [Test]
    public void Excluir_ModeloNaoEncontrado_MensagemGenericaOuOrganizacional()
    {
        // Garante que a mensagem de erro não revela que o registro existe em outro tenant
        _repo.Setup(r => r.ObterPorIdOuNulo(99, EstabB)).ReturnsAsync((ModeloDescricaoCirurgica?)null);

        var sut = new ExcluirModeloDescricaoCirurgicaCommandHandler(_repo.Object);
        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new ExcluirModeloDescricaoCirurgicaCommand
        {
            ModeloId = 99,
            EstabelecimentoId = EstabB
        }));
        // CA9/CA12: mensagem não menciona "estabelecimento A" ou "outro tenant"
        Assert.That(ex.Message, Does.Not.Contain("estabelecimento"));
    }
}
