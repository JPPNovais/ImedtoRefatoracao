using Imedto.Backend.Application.Estabelecimentos.Commands;
using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Estabelecimentos;

[TestFixture]
public class AtualizarEstabelecimentoCommandHandlerTests
{
    private Mock<IEstabelecimentoRepository> _repo;
    private Mock<IModeloPermissaoRepository> _permissoes;
    private AtualizarEstabelecimentoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _adminComPermissao = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IEstabelecimentoRepository>();
        _permissoes = new Mock<IModeloPermissaoRepository>();
        _sut = new AtualizarEstabelecimentoCommandHandler(_repo.Object, _permissoes.Object);

        _permissoes
            .Setup(p => p.UsuarioTemPermissaoExtra(
                _donoId, EstabelecimentoId, PermissoesExtras.ConfigEstabelecimento,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _permissoes
            .Setup(p => p.UsuarioTemPermissaoExtra(
                _adminComPermissao, EstabelecimentoId, PermissoesExtras.ConfigEstabelecimento,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _permissoes
            .Setup(p => p.UsuarioTemPermissaoExtra(
                _outroId, EstabelecimentoId, PermissoesExtras.ConfigEstabelecimento,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private Estabelecimento Estab() =>
        Estabelecimento.Criar(_donoId, "Original", null, null, null, null);

    // CNPJ válido usado nos testes (DV correto: 11222333000181 = 11.222.333/0001-81).
    private const string CnpjTesteFormatado = "11.222.333/0001-81";
    private const string CnpjTesteCanonico  = "11222333000181";

    private AtualizarEstabelecimentoCommand Cmd(Guid? solicitante = null) => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        UsuarioSolicitanteId = solicitante ?? _donoId,
        NomeFantasia = "Atualizado",
        RazaoSocial = "Imedto LTDA",
        Cnpj = CnpjTesteFormatado,
        Telefone = "11888887777",
        Endereco = "Rua B",
    };

    [Test]
    public async Task Handle_DonoAtualiza_PersisteAlteracoes()
    {
        var estab = Estab();
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);
        _repo.Setup(r => r.ExisteCnpj(CnpjTesteCanonico, estab.Id)).ReturnsAsync(false);

        await _sut.Handle(Cmd());

        Assert.That(estab.NomeFantasia, Is.EqualTo("Atualizado"));
        Assert.That(estab.Cnpj, Is.EqualTo(CnpjTesteCanonico));
        _repo.Verify(r => r.Salvar(estab), Times.Once);
    }

    [Test]
    public async Task Handle_AdminComPermissaoConfigEstabelecimento_PodeAtualizar()
    {
        // Admin (não-dono) com `config_estabelecimento` também consegue atualizar.
        var estab = Estab();
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);
        _repo.Setup(r => r.ExisteCnpj(CnpjTesteCanonico, estab.Id)).ReturnsAsync(false);

        await _sut.Handle(Cmd(solicitante: _adminComPermissao));

        Assert.That(estab.NomeFantasia, Is.EqualTo("Atualizado"));
        _repo.Verify(r => r.Salvar(estab), Times.Once);
    }

    [Test]
    public void Handle_UsuarioSemPermissao_LancaBusinessExceptionGenerica()
    {
        // Mensagem genérica (LGPD/multi-tenant).
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroId)));
        Assert.That(ex.Message, Does.Contain("permissão"));
        _repo.Verify(r => r.Salvar(It.IsAny<Estabelecimento>()), Times.Never);
    }

    [Test]
    public void Handle_EstabelecimentoInexistente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync((Estabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }

    [Test]
    public void Handle_CnpjJaUsadoPorOutroEstab_LancaBusinessException()
    {
        var estab = Estab();
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);
        _repo.Setup(r => r.ExisteCnpj(CnpjTesteCanonico, estab.Id)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("CNPJ"));
    }
}
