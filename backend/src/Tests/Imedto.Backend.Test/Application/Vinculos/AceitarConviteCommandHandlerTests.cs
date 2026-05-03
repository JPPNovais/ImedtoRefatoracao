using Imedto.Backend.Application.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Vinculos;

[TestFixture]
public class AceitarConviteCommandHandlerTests
{
    private Mock<IVinculoRepository> _repo;
    private Mock<IEventBus> _eventBus;
    private AceitarConviteCommandHandler _sut;

    private readonly Guid _profissionalId = Guid.NewGuid();
    private readonly Guid _outroUsuarioId = Guid.NewGuid();
    private const long VinculoId = 42;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IVinculoRepository>();
        _eventBus = new Mock<IEventBus>();
        _sut = new AceitarConviteCommandHandler(_repo.Object, _eventBus.Object);
    }

    private VinculoProfissionalEstabelecimento CriarConvite() =>
        VinculoProfissionalEstabelecimento.Convidar(
            profissionalUsuarioId: _profissionalId,
            estabelecimentoId: 1L,
            modeloPermissaoId: 10L,
            convidadoPorUsuarioId: Guid.NewGuid());

    [Test]
    public async Task Handle_ProfissionalAceitaProprioConvite_AtivaEPublicaEvento()
    {
        var vinculo = CriarConvite();
        _repo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(vinculo);

        await _sut.Handle(new AceitarConviteCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _profissionalId,
        });

        Assert.That(vinculo.Status, Is.EqualTo(VinculoStatus.Ativo));
        Assert.That(vinculo.AceitoEm, Is.Not.Null);
        _repo.Verify(r => r.Salvar(vinculo), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is VinculoAceitoEvent)), Times.Once);
    }

    [Test]
    public void Handle_VinculoInexistente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync((VinculoProfissionalEstabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new AceitarConviteCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _profissionalId,
        }));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
        _repo.Verify(r => r.Salvar(It.IsAny<VinculoProfissionalEstabelecimento>()), Times.Never);
    }

    [Test]
    public void Handle_OutroUsuarioTentaAceitar_LancaBusinessExceptionGenerica()
    {
        var vinculo = CriarConvite();
        _repo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(vinculo);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new AceitarConviteCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _outroUsuarioId,
        }));
        Assert.That(ex.Message, Is.EqualTo("Convite não encontrado."),
            "Mensagem generica — nao vaza existencia do convite para terceiros.");
        _repo.Verify(r => r.Salvar(It.IsAny<VinculoProfissionalEstabelecimento>()), Times.Never);
    }
}
