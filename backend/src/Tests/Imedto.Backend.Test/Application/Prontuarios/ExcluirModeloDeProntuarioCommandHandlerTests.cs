using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

[TestFixture]
public class ExcluirModeloDeProntuarioCommandHandlerTests
{
    private Mock<IModeloDeProntuarioRepository> _repo;
    private ExcluirModeloDeProntuarioCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long ModeloId = 50;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IModeloDeProntuarioRepository>();
        _sut = new ExcluirModeloDeProntuarioCommandHandler(_repo.Object);
    }

    private static ModeloDeProntuario ModeloDoEstab(long estabId) =>
        ModeloDeProntuario.CriarDoEstabelecimento(estabId, "Modelo", null, "{}");

    private static ModeloDeProntuario ModeloPadrao() =>
        ModeloDeProntuario.CriarPadraoSistema("Padrao", null, "{}");

    [Test]
    public async Task Handle_DoMesmoTenant_RemoveModelo()
    {
        var modelo = ModeloDoEstab(EstabelecimentoId);
        _repo.Setup(r => r.ObterVisivelOuNulo(ModeloId, EstabelecimentoId)).ReturnsAsync(modelo);

        await _sut.Handle(new ExcluirModeloDeProntuarioCommand
        {
            ModeloId = ModeloId,
            EstabelecimentoId = EstabelecimentoId,
        });

        _repo.Verify(r => r.Excluir(modelo), Times.Once);
    }

    [Test]
    public void Handle_PadraoSistema_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterVisivelOuNulo(ModeloId, EstabelecimentoId)).ReturnsAsync(ModeloPadrao());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ExcluirModeloDeProntuarioCommand
        {
            ModeloId = ModeloId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Does.Contain("padrão"));
        _repo.Verify(r => r.Excluir(It.IsAny<ModeloDeProntuario>()), Times.Never);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        // Repo filtra por tenant: chamado com EstabelecimentoId, retorna null.
        _repo.Setup(r => r.ObterVisivelOuNulo(ModeloId, EstabelecimentoId)).ReturnsAsync((ModeloDeProntuario?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ExcluirModeloDeProntuarioCommand
        {
            ModeloId = ModeloId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Is.EqualTo("Modelo não encontrado."));
    }
}
