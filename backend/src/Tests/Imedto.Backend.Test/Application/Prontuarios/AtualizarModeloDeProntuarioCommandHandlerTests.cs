using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

[TestFixture]
public class AtualizarModeloDeProntuarioCommandHandlerTests
{
    private Mock<IModeloDeProntuarioRepository> _repo;
    private AtualizarModeloDeProntuarioCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ModeloId = 50;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IModeloDeProntuarioRepository>();
        _sut = new AtualizarModeloDeProntuarioCommandHandler(_repo.Object);
    }

    private static ModeloDeProntuario ModeloDoEstab(long estabId) =>
        ModeloDeProntuario.CriarDoEstabelecimento(estabId, "Modelo", null, "{}");

    private static ModeloDeProntuario ModeloPadrao() =>
        ModeloDeProntuario.CriarPadraoSistema("Padrao", null, "{}");

    private AtualizarModeloDeProntuarioCommand Cmd() => new()
    {
        ModeloId = ModeloId,
        EstabelecimentoId = EstabelecimentoId,
        Nome = "Atualizado",
        Descricao = "Nova",
        EstruturaJson = "{\"v\":2}",
    };

    [Test]
    public async Task Handle_DoMesmoTenant_AtualizaCampos()
    {
        var modelo = ModeloDoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterVisivelOuNulo(ModeloId, EstabelecimentoId)).ReturnsAsync(modelo);

        await _sut.Handle(Cmd());

        Assert.That(modelo.Nome, Is.EqualTo("Atualizado"));
        Assert.That(modelo.EstruturaJson, Is.EqualTo("{\"v\":2}"));
        _repo.Verify(r => r.Salvar(modelo), Times.Once);
    }

    [Test]
    public void Handle_PadraoSistema_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterVisivelOuNulo(ModeloId, EstabelecimentoId)).ReturnsAsync(ModeloPadrao());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("padrão-sistema"));
        _repo.Verify(r => r.Salvar(It.IsAny<ModeloDeProntuario>()), Times.Never);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        // Repo filtra por tenant: chamado com EstabelecimentoId, retorna null.
        _repo.Setup(r => r.ObterVisivelOuNulo(ModeloId, EstabelecimentoId)).ReturnsAsync((ModeloDeProntuario?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Modelo não encontrado."));
    }

    [Test]
    public void Handle_Inexistente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterVisivelOuNulo(ModeloId, EstabelecimentoId)).ReturnsAsync((ModeloDeProntuario)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }
}
