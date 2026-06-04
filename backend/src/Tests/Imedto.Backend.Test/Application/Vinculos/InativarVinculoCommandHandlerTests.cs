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
public class InativarVinculoCommandHandlerTests
{
    private Mock<IVinculoRepository> _vinculoRepo;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private Mock<IModeloPermissaoRepository> _permissoes;
    private InativarVinculoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _profissionalId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private readonly Guid _gerirProfissionaisId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long VinculoId = 50;

    [SetUp]
    public void SetUp()
    {
        _vinculoRepo = new Mock<IVinculoRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _permissoes = new Mock<IModeloPermissaoRepository>();

        // Por padrão: apenas o Dono tem permissão (UsuarioTemPermissaoExtra retorna false para outros).
        _permissoes
            .Setup(p => p.UsuarioTemPermissaoExtra(It.IsAny<Guid>(), EstabelecimentoId, PermissoesExtras.GerirProfissionais, default))
            .ReturnsAsync(false);
        _permissoes
            .Setup(p => p.UsuarioTemPermissaoExtra(_donoId, EstabelecimentoId, PermissoesExtras.GerirProfissionais, default))
            .ReturnsAsync(true); // Dono é pass-through no repositório real; simulamos aqui.
        _permissoes
            .Setup(p => p.UsuarioTemPermissaoExtra(_gerirProfissionaisId, EstabelecimentoId, PermissoesExtras.GerirProfissionais, default))
            .ReturnsAsync(true);

        _sut = new InativarVinculoCommandHandler(_vinculoRepo.Object, _estabRepo.Object, _permissoes.Object);
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

    /// <summary>CA9 — não-Dono com gerir_profissionais pode inativar vínculo de terceiro.</summary>
    [Test]
    public async Task Handle_NaoDonoComGerirProfissionais_Inativa()
    {
        var v = Vinculo();
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(new InativarVinculoCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _gerirProfissionaisId,
        });

        Assert.That(v.Status, Is.EqualTo(VinculoStatus.Inativo));
        _vinculoRepo.Verify(r => r.Salvar(v), Times.Once);
    }

    /// <summary>CA11 — terceiro sem gerir_profissionais recebe 422.</summary>
    [Test]
    public void Handle_TerceiroSemPermissao_LancaBusinessException()
    {
        var v = Vinculo();
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(v);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new InativarVinculoCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _outroId,
        }));
        Assert.That(ex.Message, Does.Contain("permissão"));
        _vinculoRepo.Verify(r => r.Salvar(It.IsAny<VinculoProfissionalEstabelecimento>()), Times.Never);
    }

    /// <summary>CA6 — Dono não pode ser desativado mesmo por quem tem gerir_profissionais.</summary>
    [Test]
    public void Handle_TentaInativarDono_LancaBusinessException()
    {
        // Simula situação onde o profissional do vínculo é o Dono.
        // _outroId convida _donoId para satisfazer a regra "não pode convidar a si mesmo".
        var vinculoDono = VinculoProfissionalEstabelecimento.Convidar(
            _donoId, EstabelecimentoId, 1L, _outroId);
        vinculoDono.Aceitar();

        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(vinculoDono);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new InativarVinculoCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _gerirProfissionaisId,
        }));
        Assert.That(ex.Message, Does.Contain("dono").IgnoreCase);
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
