using Imedto.Backend.Application.ModelosPermissao.Commands;
using Imedto.Backend.Contracts.ModelosPermissao.Commands;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.ModelosPermissao;

[TestFixture]
public class AtualizarModeloPermissaoCommandHandlerTests
{
    private Mock<IModeloPermissaoRepository> _repo;
    private AtualizarModeloPermissaoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ModeloId = 99;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IModeloPermissaoRepository>();
        _sut = new AtualizarModeloPermissaoCommandHandler(_repo.Object);
    }

    private ModeloPermissaoEstabelecimento ModeloDoEstab(long estabId, bool ehPadrao = false)
    {
        if (ehPadrao)
            return ModeloPermissaoEstabelecimento.CriarPadroes(estabId).First(m => m.Nome == "Admin");

        return ModeloPermissaoEstabelecimento.Criar(estabId, "Coord", TipoAcessoModelo.Profissional);
    }

    private AtualizarModeloPermissaoCommand Cmd() => new()
    {
        ModeloId = ModeloId,
        EstabelecimentoId = EstabelecimentoId,
        Nome = "Atualizado",
        TipoAcesso = "Profissional",
        Permissoes = new[] { "agenda" },
    };

    [Test]
    public async Task Handle_ModeloDoMesmoTenant_AtualizaCampos()
    {
        var modelo = ModeloDoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(ModeloId)).ReturnsAsync(modelo);

        await _sut.Handle(Cmd());

        Assert.That(modelo.Nome, Is.EqualTo("Atualizado"));
        Assert.That(modelo.Permissoes, Is.EquivalentTo(new[] { "agenda" }));
        _repo.Verify(r => r.Salvar(modelo), Times.Once);
    }

    [Test]
    public void Handle_TipoAcessoInvalido_LancaBusinessException()
    {
        var cmd = Cmd();
        cmd.TipoAcesso = "Hacker";

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("TipoAcesso inválido"));
    }

    [Test]
    public void Handle_ModeloDeOutroTenant_LancaMensagemGenericaENaoSalva()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ModeloId)).ReturnsAsync(ModeloDoEstab(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Modelo não encontrado."));
        _repo.Verify(r => r.Salvar(It.IsAny<ModeloPermissaoEstabelecimento>()), Times.Never);
    }

    [Test]
    public void Handle_ModeloPadraoNaoPodeSerAlterado_LancaBusinessExceptionDoAggregate()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ModeloId)).ReturnsAsync(ModeloDoEstab(EstabelecimentoId, ehPadrao: true));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("padrão"));
        _repo.Verify(r => r.Salvar(It.IsAny<ModeloPermissaoEstabelecimento>()), Times.Never);
    }

    [Test]
    public void Handle_ModeloInexistente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(ModeloId)).ReturnsAsync((ModeloPermissaoEstabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }
}
