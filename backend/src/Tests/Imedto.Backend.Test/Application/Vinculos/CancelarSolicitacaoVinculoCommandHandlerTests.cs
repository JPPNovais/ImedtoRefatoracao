using Imedto.Backend.Application.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Vinculos;

[TestFixture]
public class CancelarSolicitacaoVinculoCommandHandlerTests
{
    private Mock<ISolicitacaoVinculoRepository> _solicitacaoRepo;
    private CancelarSolicitacaoVinculoCommandHandler _sut;

    private readonly Guid _profissionalId = Guid.NewGuid();
    private readonly Guid _outroUsuarioId = Guid.NewGuid();
    private const long SolicitacaoId = 99;

    [SetUp]
    public void SetUp()
    {
        _solicitacaoRepo = new Mock<ISolicitacaoVinculoRepository>();
        _sut = new CancelarSolicitacaoVinculoCommandHandler(_solicitacaoRepo.Object);
    }

    private SolicitacaoVinculo Solicitacao() =>
        SolicitacaoVinculo.Solicitar(_profissionalId, 1L, "Pedido");

    [Test]
    public async Task Handle_ProprioProfissional_Cancela()
    {
        var s = Solicitacao();
        _solicitacaoRepo.Setup(r => r.ObterPorIdOuNulo(SolicitacaoId)).ReturnsAsync(s);

        await _sut.Handle(new CancelarSolicitacaoVinculoCommand
        {
            SolicitacaoId = SolicitacaoId,
            SolicitanteUsuarioId = _profissionalId,
        });

        Assert.That(s.Status, Is.EqualTo(StatusSolicitacaoVinculo.Cancelada));
        _solicitacaoRepo.Verify(r => r.Salvar(s), Times.Once);
    }

    [Test]
    public void Handle_OutroUsuarioTentaCancelar_LancaMensagemGenerica()
    {
        var s = Solicitacao();
        _solicitacaoRepo.Setup(r => r.ObterPorIdOuNulo(SolicitacaoId)).ReturnsAsync(s);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new CancelarSolicitacaoVinculoCommand
        {
            SolicitacaoId = SolicitacaoId,
            SolicitanteUsuarioId = _outroUsuarioId,
        }));
        Assert.That(ex.Message, Is.EqualTo("Solicitação não encontrada."),
            "Cross-user: mesma mensagem de inexistente — defense-in-depth contra enumeration.");
        _solicitacaoRepo.Verify(r => r.Salvar(It.IsAny<SolicitacaoVinculo>()), Times.Never);
    }

    [Test]
    public void Handle_SolicitacaoInexistente_LancaBusinessException()
    {
        _solicitacaoRepo.Setup(r => r.ObterPorIdOuNulo(SolicitacaoId))
                        .ReturnsAsync((SolicitacaoVinculo)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new CancelarSolicitacaoVinculoCommand
        {
            SolicitacaoId = SolicitacaoId,
            SolicitanteUsuarioId = _profissionalId,
        }));
        Assert.That(ex.Message, Does.Contain("não encontrada"));
    }
}
