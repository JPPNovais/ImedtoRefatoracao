using Imedto.Backend.Application.Profissionais.Commands;
using Imedto.Backend.Contracts.Profissionais.Commands;
using Imedto.Backend.Domain.Common;
using Imedto.Backend.Domain.Profissionais;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Profissionais;

[TestFixture]
public class RemoverFotoProfissionalCommandHandlerTests
{
    private Mock<IProfissionalRepository> _repo;
    private Mock<IFotoStorageService> _storage;
    private RemoverFotoProfissionalCommandHandler _sut;
    private readonly Guid _usuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IProfissionalRepository>();
        _storage = new Mock<IFotoStorageService>();
        _sut = new RemoverFotoProfissionalCommandHandler(_repo.Object, _storage.Object);
    }

    private Profissional ProfComFoto(string fotoUrl = "https://cdn.example/profissionais/abc.png?sig=xyz")
    {
        var prof = Profissional.Cadastrar(_usuarioId, "CRM", "SP", "1", null, null);
        prof.AlterarFoto(fotoUrl);
        return prof;
    }

    private RemoverFotoProfissionalCommand Cmd(Guid? id = null) => new()
    {
        UsuarioId = id ?? _usuarioId,
    };

    [Test]
    public async Task Handle_ComFoto_RemoveNoStorageEZeraFotoUrl()
    {
        var prof = ProfComFoto($"https://cdn.example/profissionais/{_usuarioId}.png?sig=abc");
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(prof);

        await _sut.Handle(Cmd());

        Assert.That(prof.FotoUrl, Is.Null, "Aggregate deve zerar FotoUrl apos remover.");
        _storage.Verify(s => s.RemoverFotoAsync(
            $"profissionais/{_usuarioId}.png",
            It.IsAny<CancellationToken>()), Times.Once,
            "Storage deve receber path profissionais/{usuarioId}.{ext}.");
        _repo.Verify(r => r.Salvar(prof), Times.Once);
    }

    [Test]
    public async Task Handle_SemFotoPrevia_NaoTocaStorageNemPersiste()
    {
        // Idempotente: já não havia foto — não toca storage nem salva (audit limpo).
        var prof = Profissional.Cadastrar(_usuarioId, "CRM", "SP", "1", null, null);
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(prof);

        await _sut.Handle(Cmd());

        Assert.That(prof.FotoUrl, Is.Null);
        _storage.Verify(s => s.RemoverFotoAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never,
            "Sem foto previa nao deve invocar o storage.");
        _repo.Verify(r => r.Salvar(It.IsAny<Profissional>()), Times.Never,
            "Sem foto previa nao deve persistir.");
    }

    [Test]
    public void Handle_UsuarioGuidEmpty_LancaBusinessException()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(id: Guid.Empty)));
        Assert.That(ex.Message, Does.Contain("não identificado"));
        _storage.Verify(s => s.RemoverFotoAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Handle_PerfilInexistente_LancaBusinessExceptionGenerica()
    {
        // Mensagem generica — LGPD/multi-tenant.
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync((Profissional)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
        _storage.Verify(s => s.RemoverFotoAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_FotoUrlComQueryStringPresigned_ExtraiExtensaoCorreta()
    {
        // Presigned URL S3 — extrair extensao do path ignorando query string.
        var prof = ProfComFoto($"https://bucket.s3.amazonaws.com/profissionais/{_usuarioId}.webp?X-Amz-Signature=abc&Expires=999");
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(prof);

        await _sut.Handle(Cmd());

        _storage.Verify(s => s.RemoverFotoAsync(
            $"profissionais/{_usuarioId}.webp",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_FotoUrlSemExtensao_FazFallbackParaJpg()
    {
        var prof = ProfComFoto("https://cdn.example/foto");
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(prof);

        await _sut.Handle(Cmd());

        _storage.Verify(s => s.RemoverFotoAsync(
            $"profissionais/{_usuarioId}.jpg",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

[TestFixture]
public class ProfissionalRemoverFotoTests
{
    private Profissional NovoProf() =>
        Profissional.Cadastrar(Guid.NewGuid(), "CRM", "SP", "1", null, null);

    [Test]
    public void RemoverFoto_ComFoto_ZeraFotoUrlEAtualizaTimestamp()
    {
        var prof = NovoProf();
        prof.AlterarFoto("https://cdn.example/x.png");
        var antesUpdate = prof.AtualizadoEm;

        prof.RemoverFoto();

        Assert.That(prof.FotoUrl, Is.Null);
        Assert.That(prof.AtualizadoEm, Is.Not.EqualTo(antesUpdate), "AtualizadoEm deve refletir a remocao.");
    }

    [Test]
    public void RemoverFoto_SemFoto_NaoMudaTimestamp()
    {
        // Idempotente — sem audit ruido quando ja nao havia foto.
        var prof = NovoProf();
        var antes = prof.AtualizadoEm;

        prof.RemoverFoto();

        Assert.That(prof.FotoUrl, Is.Null);
        Assert.That(prof.AtualizadoEm, Is.EqualTo(antes),
            "AtualizadoEm nao deve mudar quando nao havia foto.");
    }
}
