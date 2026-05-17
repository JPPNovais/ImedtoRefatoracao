using System.Text;
using Imedto.Backend.Application.Estabelecimentos.Commands;
using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Common;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Estabelecimentos;

[TestFixture]
public class AlterarFotoEstabelecimentoCommandHandlerTests
{
    private Mock<IEstabelecimentoRepository> _repo;
    private Mock<IFotoStorageService> _storage;
    private Mock<IModeloPermissaoRepository> _permissoes;
    private AlterarFotoEstabelecimentoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _adminComPermissao = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IEstabelecimentoRepository>();
        _storage = new Mock<IFotoStorageService>();
        _permissoes = new Mock<IModeloPermissaoRepository>();
        _sut = new AlterarFotoEstabelecimentoCommandHandler(_repo.Object, _storage.Object, _permissoes.Object);

        // Default: Dono e Admin com permissão passam; outros não passam.
        // UsuarioTemPermissaoExtra já trata Dono como pass-through na infra real —
        // aqui mockamos a mesma semântica para refletir o contrato.
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
        Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);

    private AlterarFotoEstabelecimentoCommand Cmd(string ext = "png", Guid? solicitante = null) => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        UsuarioSolicitanteId = solicitante ?? _donoId,
        MimeType = "image/png",
        Extensao = ext,
        Conteudo = new MemoryStream(Encoding.UTF8.GetBytes("fake-bytes")),
    };

    [Test]
    public async Task Handle_DonoEnviaFoto_SobeNoStorageEAtualizaUrl()
    {
        var estab = Estab();
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);
        _storage.Setup(s => s.UploadFotoAsync(
                $"estabelecimentos/{EstabelecimentoId}.png",
                It.IsAny<Stream>(),
                "image/png",
                It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://cdn/foto.png");

        await _sut.Handle(Cmd());

        Assert.That(estab.FotoUrl, Is.EqualTo("https://cdn/foto.png"));
        _repo.Verify(r => r.Salvar(estab), Times.Once);
    }

    [Test]
    public async Task Handle_AdminComPermissaoConfigEstabelecimento_PodeAlterarFoto()
    {
        // Admin (não-dono) que tem permissão fina `config_estabelecimento` no estabelecimento
        // também consegue alterar — alinha com o RequiresPermissaoExtra do controller.
        var estab = Estab();
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);
        _storage.Setup(s => s.UploadFotoAsync(
                $"estabelecimentos/{EstabelecimentoId}.png",
                It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://cdn/foto.png");

        await _sut.Handle(Cmd(solicitante: _adminComPermissao));

        Assert.That(estab.FotoUrl, Is.EqualTo("https://cdn/foto.png"));
        _repo.Verify(r => r.Salvar(estab), Times.Once);
    }

    [Test]
    public async Task Handle_ExtensaoComPontoEMaiusculo_NormalizaParaLowerSemPonto()
    {
        var estab = Estab();
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);
        _storage.Setup(s => s.UploadFotoAsync(
                $"estabelecimentos/{EstabelecimentoId}.jpg",
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://cdn/foto.jpg");

        await _sut.Handle(Cmd(ext: ".JPG"));

        _storage.Verify(s => s.UploadFotoAsync(
            $"estabelecimentos/{EstabelecimentoId}.jpg",
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void Handle_UsuarioSemPermissao_LancaBusinessExceptionGenerica()
    {
        // Mensagem genérica, sem revelar se é por papel ou por outro tenant (LGPD/multi-tenant).
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroId)));
        Assert.That(ex.Message, Does.Contain("permissão"));
        _storage.Verify(s => s.UploadFotoAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Storage nao deve ser tocado quando autorizacao falha — desperdicio + risco.");
    }

    [Test]
    public void Handle_EstabelecimentoInexistente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync((Estabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }
}
