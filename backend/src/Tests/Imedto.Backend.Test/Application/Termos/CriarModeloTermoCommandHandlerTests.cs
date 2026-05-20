using Imedto.Backend.Application.Termos.Commands;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Termos;

[TestFixture]
public class CriarModeloTermoCommandHandlerTests
{
    private Mock<ITermoModeloRepository> _repo = null!;
    private Mock<ITermoHtmlSanitizer> _sanitizer = null!;
    private Mock<ITermoAuditLogger> _audit = null!;
    private CriarModeloTermoCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ITermoModeloRepository>();
        _sanitizer = new Mock<ITermoHtmlSanitizer>();
        _audit = new Mock<ITermoAuditLogger>();
        _sut = new CriarModeloTermoCommandHandler(_repo.Object, _sanitizer.Object, _audit.Object);
    }

    private CriarModeloTermoCommand Cmd() => new()
    {
        EstabelecimentoId = 1,
        SolicitanteUsuarioId = Guid.NewGuid(),
        Categoria = "lgpd",
        Titulo = "Termo LGPD",
        ConteudoHtml = "<p>conteudo</p>",
    };

    [Test]
    public async Task Handle_FluxoValido_PersisteModeloESnapshotV1()
    {
        _sanitizer.Setup(s => s.Sanitizar(It.IsAny<string>())).Returns("<p>conteudo</p>");
        _repo.Setup(r => r.Salvar(It.IsAny<TermoModelo>()))
            .Callback<TermoModelo>(m => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(m, 42L))
            .Returns(Task.CompletedTask);

        var cmd = Cmd();
        await _sut.Handle(cmd);

        Assert.That(cmd.ModeloIdCriado, Is.EqualTo(42L));
        _repo.Verify(r => r.SalvarVersao(It.Is<TermoModeloVersao>(v => v.TermoModeloId == 42L && v.Versao == 1)), Times.Once);
        _audit.Verify(a => a.RegistrarAsync(1, It.IsAny<Guid?>(), "modelo-criado", "TermoModelo", 42L,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Handle_SanitizerLimpaConteudoCompletamente_LancaBusinessException()
    {
        _sanitizer.Setup(s => s.Sanitizar(It.IsAny<string>())).Returns("   ");

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
    }

    [Test]
    public void Handle_CategoriaInvalida_LancaBusinessException()
    {
        var cmd = Cmd();
        cmd.Categoria = "categoria_xpto";
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
    }
}
