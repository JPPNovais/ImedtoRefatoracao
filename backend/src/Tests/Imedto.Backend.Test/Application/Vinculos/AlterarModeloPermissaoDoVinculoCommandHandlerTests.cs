using Imedto.Backend.Application.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Vinculos;

[TestFixture]
public class AlterarModeloPermissaoDoVinculoCommandHandlerTests
{
    private Mock<IVinculoRepository> _vinculoRepo;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private Mock<IModeloPermissaoRepository> _modeloRepo;
    private AlterarModeloPermissaoDoVinculoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long VinculoId = 50;
    private const long NovoModeloId = 99;

    [SetUp]
    public void SetUp()
    {
        _vinculoRepo = new Mock<IVinculoRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _modeloRepo = new Mock<IModeloPermissaoRepository>();
        _sut = new AlterarModeloPermissaoDoVinculoCommandHandler(
            _vinculoRepo.Object, _estabRepo.Object, _modeloRepo.Object);
    }

    private VinculoProfissionalEstabelecimento VinculoNoEstab(long estabId)
    {
        var v = VinculoProfissionalEstabelecimento.Convidar(
            Guid.NewGuid(), estabId, 1L, _donoId);
        v.Aceitar();
        return v;
    }

    private Estabelecimento Estab() =>
        Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);

    private AlterarModeloPermissaoDoVinculoCommand Cmd(Guid? solicitante = null) => new()
    {
        VinculoId = VinculoId,
        EstabelecimentoId = EstabelecimentoId,
        NovoModeloPermissaoId = NovoModeloId,
        UsuarioSolicitanteId = solicitante ?? _donoId,
    };

    [Test]
    public async Task Handle_DonoAlteraModeloDoVinculo_AtualizaModelo()
    {
        var v = VinculoNoEstab(EstabelecimentoId);
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());
        _modeloRepo.Setup(r => r.PertenceAoEstabelecimento(NovoModeloId, EstabelecimentoId))
                   .ReturnsAsync(true);

        await _sut.Handle(Cmd());

        Assert.That(v.ModeloPermissaoId, Is.EqualTo(NovoModeloId));
        _vinculoRepo.Verify(r => r.Salvar(v), Times.Once);
    }

    [Test]
    public void Handle_VinculoCrossTenant_LancaMensagemGenerica()
    {
        // Vinculo pertence a OUTRO estab — handler deve cortar antes de checar dono.
        var v = VinculoNoEstab(OutroEstabId);
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(v);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Vínculo não encontrado."));
        _estabRepo.Verify(r => r.ObterPorId(It.IsAny<long>()), Times.Never,
            "Curto-circuito antes de bater no estab repo.");
    }

    [Test]
    public void Handle_NaoEhDono_LancaBusinessException()
    {
        var v = VinculoNoEstab(EstabelecimentoId);
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroId)));
        Assert.That(ex.Message, Does.Contain("dono"));
    }

    [Test]
    public void Handle_ModeloDeOutroEstab_LancaBusinessException()
    {
        var v = VinculoNoEstab(EstabelecimentoId);
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());
        _modeloRepo.Setup(r => r.PertenceAoEstabelecimento(NovoModeloId, EstabelecimentoId))
                   .ReturnsAsync(false);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não pertence"));
    }

    [Test]
    public void Handle_VinculoInexistente_LancaBusinessException()
    {
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId))
                    .ReturnsAsync((VinculoProfissionalEstabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }
}
