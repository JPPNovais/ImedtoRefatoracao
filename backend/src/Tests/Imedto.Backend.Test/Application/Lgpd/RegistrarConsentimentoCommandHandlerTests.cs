using Imedto.Backend.Application.Lgpd.Commands;
using Imedto.Backend.Contracts.Lgpd.Commands;
using Imedto.Backend.Domain.Lgpd;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Lgpd;

[TestFixture]
public class RegistrarConsentimentoCommandHandlerTests
{
    private Mock<ILgpdConsentimentoRepository> _repo;
    private RegistrarConsentimentoCommandHandler _sut;
    private readonly Guid _usuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ILgpdConsentimentoRepository>();
        _sut = new RegistrarConsentimentoCommandHandler(_repo.Object);
    }

    private RegistrarConsentimentoCommand Cmd(string tipo = "TermosUso") => new()
    {
        UsuarioId = _usuarioId,
        Tipo = tipo,
        Versao = "v1.0",
        IpOrigem = "192.168.0.1",
        UserAgent = "Mozilla",
    };

    [Test]
    public async Task Handle_TipoValido_PersisteConsentimento()
    {
        await _sut.Handle(Cmd("TermosUso"));

        _repo.Verify(r => r.Salvar(It.Is<LgpdConsentimento>(c =>
            c.UsuarioId == _usuarioId &&
            c.Tipo == TipoConsentimentoLgpd.TermosUso &&
            c.Versao == "v1.0" &&
            c.IpOrigem == "192.168.0.1")),
            Times.Once);
    }

    [Test]
    public async Task Handle_TipoCaseInsensitive_AceitaPoliticaPrivacidade()
    {
        await _sut.Handle(Cmd("politicaprivacidade"));

        _repo.Verify(r => r.Salvar(It.Is<LgpdConsentimento>(c =>
            c.Tipo == TipoConsentimentoLgpd.PoliticaPrivacidade)),
            Times.Once);
    }

    [Test]
    public void Handle_TipoInvalido_LancaBusinessException()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd("Inexistente")));
        Assert.That(ex.Message, Does.Contain("Tipo de consentimento inválido"));
        _repo.Verify(r => r.Salvar(It.IsAny<LgpdConsentimento>()), Times.Never);
    }

    [Test]
    public void Handle_VersaoVazia_LancaBusinessExceptionDoAggregate()
    {
        var cmd = new RegistrarConsentimentoCommand
        {
            UsuarioId = _usuarioId,
            Tipo = "TermosUso",
            Versao = " ",
            IpOrigem = null,
            UserAgent = null,
        };

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("Versão"));
        _repo.Verify(r => r.Salvar(It.IsAny<LgpdConsentimento>()), Times.Never);
    }
}
