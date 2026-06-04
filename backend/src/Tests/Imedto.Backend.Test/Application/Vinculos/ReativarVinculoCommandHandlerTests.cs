using Imedto.Backend.Application.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Vinculos;

[TestFixture]
public class ReativarVinculoCommandHandlerTests
{
    private Mock<IVinculoRepository> _vinculoRepo;
    private Mock<IModeloPermissaoRepository> _permissoes;
    private ReativarVinculoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _profissionalId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private readonly Guid _gerirProfissionaisId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long VinculoId = 51;

    [SetUp]
    public void SetUp()
    {
        _vinculoRepo = new Mock<IVinculoRepository>();
        _permissoes = new Mock<IModeloPermissaoRepository>();

        // Por padrão: nenhum usuário tem permissão (cenário de negação).
        _permissoes
            .Setup(p => p.UsuarioTemPermissaoExtra(It.IsAny<Guid>(), EstabelecimentoId, PermissoesExtras.GerirProfissionais, default))
            .ReturnsAsync(false);
        // Dono é pass-through no repositório real; simulamos aqui.
        _permissoes
            .Setup(p => p.UsuarioTemPermissaoExtra(_donoId, EstabelecimentoId, PermissoesExtras.GerirProfissionais, default))
            .ReturnsAsync(true);
        // Usuário não-Dono com gerir_profissionais.
        _permissoes
            .Setup(p => p.UsuarioTemPermissaoExtra(_gerirProfissionaisId, EstabelecimentoId, PermissoesExtras.GerirProfissionais, default))
            .ReturnsAsync(true);

        _sut = new ReativarVinculoCommandHandler(_vinculoRepo.Object, _permissoes.Object);
    }

    private VinculoProfissionalEstabelecimento VinculoInativo()
    {
        var v = VinculoProfissionalEstabelecimento.Convidar(
            _profissionalId, EstabelecimentoId, 1L, _donoId);
        v.Aceitar();
        v.Inativar();
        return v;
    }

    /// <summary>Caminho feliz — Dono reativa vínculo aceito-e-inativado.</summary>
    [Test]
    public async Task Handle_DonoReativa_Reativa()
    {
        var v = VinculoInativo();
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(v);

        await _sut.Handle(new ReativarVinculoCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _donoId,
        });

        Assert.That(v.Status, Is.EqualTo(VinculoStatus.Ativo));
        _vinculoRepo.Verify(r => r.Salvar(v), Times.Once);
    }

    /// <summary>CA10 — não-Dono com gerir_profissionais pode reativar.</summary>
    [Test]
    public async Task Handle_NaoDonoComGerirProfissionais_Reativa()
    {
        var v = VinculoInativo();
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(v);

        await _sut.Handle(new ReativarVinculoCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _gerirProfissionaisId,
        });

        Assert.That(v.Status, Is.EqualTo(VinculoStatus.Ativo));
        _vinculoRepo.Verify(r => r.Salvar(v), Times.Once);
    }

    /// <summary>CA11 — terceiro sem permissão recebe 422.</summary>
    [Test]
    public void Handle_TerceiroSemPermissao_LancaBusinessException()
    {
        var v = VinculoInativo();
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(v);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ReativarVinculoCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _outroId,
        }));
        Assert.That(ex.Message, Does.Contain("permissão"));
        _vinculoRepo.Verify(r => r.Salvar(It.IsAny<VinculoProfissionalEstabelecimento>()), Times.Never);
    }

    /// <summary>CA8 — vínculo nunca aceito (AceitoEm == null) não pode ser reativado.</summary>
    [Test]
    public void Handle_VinculoNuncaAceito_LancaBusinessException()
    {
        // Vínculo Inativo sem aceite: criado via Convidar → Inativar (sem Aceitar).
        var v = VinculoProfissionalEstabelecimento.Convidar(
            _profissionalId, EstabelecimentoId, 1L, _donoId);
        v.Inativar();

        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId)).ReturnsAsync(v);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ReativarVinculoCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _donoId,
        }));
        Assert.That(ex.Message, Does.Contain("nunca foi aceito").IgnoreCase);
        _vinculoRepo.Verify(r => r.Salvar(It.IsAny<VinculoProfissionalEstabelecimento>()), Times.Never);
    }

    [Test]
    public void Handle_VinculoInexistente_LancaBusinessException()
    {
        _vinculoRepo.Setup(r => r.ObterPorIdOuNulo(VinculoId))
                    .ReturnsAsync((VinculoProfissionalEstabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ReativarVinculoCommand
        {
            VinculoId = VinculoId,
            UsuarioSolicitanteId = _donoId,
        }));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }
}
