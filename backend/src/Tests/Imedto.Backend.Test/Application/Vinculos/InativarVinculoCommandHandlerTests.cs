using Imedto.Backend.Application.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Vinculos;

[TestFixture]
public class InativarVinculoCommandHandlerTests
{
    private Mock<IVinculoRepository> _vinculoRepo;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private InativarVinculoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _profissionalId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long VinculoId = 50;

    [SetUp]
    public void SetUp()
    {
        _vinculoRepo = new Mock<IVinculoRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _sut = new InativarVinculoCommandHandler(_vinculoRepo.Object, _estabRepo.Object);
    }

    private VinculoProfissionalEstabelecimento Vinculo()
    {
        var v = VinculoProfissionalEstabelecimento.Convidar(
            _profissionalId, EstabelecimentoId, 1L, _donoId);
        v.Aceitar();
        return v;
    }

    private Estabelecimento Estab() =>
        Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);

    [Test]
    public async Task Handle_DonoInativaVinculoDoSeuEstab_Inativa()
    {
        var v = Vinculo();
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(new InativarVinculoCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _donoId,
        });

        Assert.That(v.Status, Is.EqualTo(VinculoStatus.Inativo));
        _vinculoRepo.Verify(r => r.Salvar(v), Times.Once);
    }

    [Test]
    public async Task Handle_ProprioProfissionalInativa_Inativa()
    {
        var v = Vinculo();
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(new InativarVinculoCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _profissionalId,
        });

        Assert.That(v.Status, Is.EqualTo(VinculoStatus.Inativo));
    }

    [Test]
    public void Handle_TerceiroTentaInativar_LancaBusinessException()
    {
        var v = Vinculo();
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new InativarVinculoCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _outroId,
        }));
        Assert.That(ex.Message, Does.Contain("dono").And.Contain("profissional"));
        _vinculoRepo.Verify(r => r.Salvar(It.IsAny<VinculoProfissionalEstabelecimento>()), Times.Never);
    }

    [Test]
    public void Handle_VinculoInexistente_LancaBusinessException()
    {
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId))
                    .ReturnsAsync((VinculoProfissionalEstabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new InativarVinculoCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _donoId,
        }));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }
}
