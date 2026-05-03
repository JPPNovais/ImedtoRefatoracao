using System.Text;
using Imedto.Backend.Application.Profissionais.Commands;
using Imedto.Backend.Contracts.Profissionais.Commands;
using Imedto.Backend.Domain.Common;
using Imedto.Backend.Domain.Profissionais;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Profissionais;

[TestFixture]
public class AlterarFotoProfissionalCommandHandlerTests
{
    private Mock<IProfissionalRepository> _repo;
    private Mock<IFotoStorageService> _storage;
    private AlterarFotoProfissionalCommandHandler _sut;
    private readonly Guid _usuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IProfissionalRepository>();
        _storage = new Mock<IFotoStorageService>();
        _sut = new AlterarFotoProfissionalCommandHandler(_repo.Object, _storage.Object);
    }

    private Profissional Profissional() =>
        Imedto.Backend.Domain.Profissionais.Profissional.Cadastrar(
            _usuarioId, "CRM", "SP", "1", null, null);

    private AlterarFotoProfissionalCommand Cmd(string ext = "png", Guid? id = null) => new()
    {
        UsuarioId = id ?? _usuarioId,
        MimeType = "image/png",
        Extensao = ext,
        Conteudo = new MemoryStream(Encoding.UTF8.GetBytes("bytes")),
    };

    [Test]
    public async Task Handle_TudoValido_SobeFotoEAtualizaUrl()
    {
        var prof = Profissional();
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(prof);
        _storage.Setup(s => s.UploadFotoAsync(
                $"profissionais/{_usuarioId}.png", It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://cdn/p.png");

        await _sut.Handle(Cmd());

        Assert.That(prof.FotoUrl, Is.EqualTo("https://cdn/p.png"));
        _repo.Verify(r => r.Salvar(prof), Times.Once);
    }

    [Test]
    public void Handle_UsuarioGuidEmpty_LancaBusinessException()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(id: Guid.Empty)));
        Assert.That(ex.Message, Does.Contain("não identificado"));
        _storage.Verify(s => s.UploadFotoAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void Handle_PerfilProfissionalInexistente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync((Profissional)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("Cadastre"));
        _storage.Verify(s => s.UploadFotoAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
