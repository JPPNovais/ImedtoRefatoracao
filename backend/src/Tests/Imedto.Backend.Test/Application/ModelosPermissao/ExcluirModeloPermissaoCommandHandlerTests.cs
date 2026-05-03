using Imedto.Backend.Application.ModelosPermissao.Commands;
using Imedto.Backend.Contracts.ModelosPermissao.Commands;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.ModelosPermissao;

[TestFixture]
public class ExcluirModeloPermissaoCommandHandlerTests
{
    private Mock<IModeloPermissaoRepository> _repo;
    private ExcluirModeloPermissaoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ModeloId = 99;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IModeloPermissaoRepository>();
        _sut = new ExcluirModeloPermissaoCommandHandler(_repo.Object);
    }

    private ModeloPermissaoEstabelecimento ModeloDoEstab(long estabId, bool ehPadrao = false)
    {
        if (ehPadrao)
            return ModeloPermissaoEstabelecimento.CriarPadroes(estabId).First(m => m.Nome == "Admin");

        return ModeloPermissaoEstabelecimento.Criar(estabId, "Coord", TipoAcessoModelo.Profissional);
    }

    private ExcluirModeloPermissaoCommand Cmd() => new()
    {
        ModeloId = ModeloId,
        EstabelecimentoId = EstabelecimentoId,
    };

    [Test]
    public async Task Handle_ModeloLivre_RemoveDoBanco()
    {
        var modelo = ModeloDoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ModeloId)).ReturnsAsync(modelo);
        _repo.Setup(r => r.EstaEmUsoPorVinculoAtivo(ModeloId)).ReturnsAsync(false);

        await _sut.Handle(Cmd());

        _repo.Verify(r => r.Excluir(modelo), Times.Once);
    }

    [Test]
    public void Handle_ModeloDeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ModeloId)).ReturnsAsync(ModeloDoEstab(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Modelo não encontrado."));
        _repo.Verify(r => r.Excluir(It.IsAny<ModeloPermissaoEstabelecimento>()), Times.Never);
    }

    [Test]
    public void Handle_ModeloPadrao_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ModeloId)).ReturnsAsync(ModeloDoEstab(EstabelecimentoId, ehPadrao: true));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("padrão"));
    }

    [Test]
    public void Handle_ModeloEmUsoPorVinculoAtivo_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ModeloId)).ReturnsAsync(ModeloDoEstab(EstabelecimentoId));
        _repo.Setup(r => r.EstaEmUsoPorVinculoAtivo(ModeloId)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("vinculados"));
        _repo.Verify(r => r.Excluir(It.IsAny<ModeloPermissaoEstabelecimento>()), Times.Never);
    }

    [Test]
    public void Handle_ModeloInexistente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ModeloId)).ReturnsAsync((ModeloPermissaoEstabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }
}
