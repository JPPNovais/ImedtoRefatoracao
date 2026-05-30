using Imedto.Backend.Application.Admin.Estabelecimentos.Commands;
using Imedto.Backend.Contracts.Admin.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Admin.Estabelecimentos;

[TestFixture]
public class ResetTenantCommandHandlerTests
{
    private Mock<IAdminResetService> _resetService;
    private Mock<IAdminEstabelecimentosQueryRepository> _repo;
    private Mock<ImedtoAdminAuditWriter> _audit;
    private ResetTenantCommandHandler _sut;

    private const long EstabelecimentoId = 42L;
    private static readonly Guid AdminId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _resetService = new Mock<IAdminResetService>();
        _repo = new Mock<IAdminEstabelecimentosQueryRepository>();
        _audit = new Mock<ImedtoAdminAuditWriter>(null!, null!, null!);
        _sut = new ResetTenantCommandHandler(_resetService.Object, _repo.Object, _audit.Object);
    }

    [Test]
    public async Task Handle_QuandoConfirmacaoCorreta_ExecutaReset()
    {
        _repo.Setup(r => r.ObterCpfENomeFantasiaAsync(EstabelecimentoId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(("12345678900", "Clínica Feliz"));

        _audit.Setup(a => a.RegistrarAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                                            It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<string?>(),
                                            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        var cmd = new ResetTenantCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            AdminId = AdminId,
            ConfirmarNomeFantasia = "Clínica Feliz",
            Motivo = "Motivo de reset válido",
        };

        await _sut.Handle(cmd);

        _resetService.Verify(r => r.ResetEstabelecimentoAsync(
            EstabelecimentoId, It.IsAny<ResetModulos>(), "Motivo de reset válido", AdminId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void Handle_QuandoNomeFantasiaDivergente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterCpfENomeFantasiaAsync(EstabelecimentoId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(("12345678900", "Clínica Feliz"));

        var cmd = new ResetTenantCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            AdminId = AdminId,
            ConfirmarNomeFantasia = "Nome Errado",
            Motivo = "Motivo de reset válido",
        };

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        _resetService.Verify(r => r.ResetEstabelecimentoAsync(
            It.IsAny<long>(), It.IsAny<ResetModulos>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void Handle_QuandoMotivoVazio_LancaBusinessException()
    {
        var cmd = new ResetTenantCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            AdminId = AdminId,
            ConfirmarNomeFantasia = "Clínica Feliz",
            Motivo = "",
        };

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        _resetService.Verify(r => r.ResetEstabelecimentoAsync(
            It.IsAny<long>(), It.IsAny<ResetModulos>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void Handle_QuandoMotivoMenorQue10Chars_LancaBusinessException()
    {
        var cmd = new ResetTenantCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            AdminId = AdminId,
            ConfirmarNomeFantasia = "Clínica Feliz",
            Motivo = "curto",
        };

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
    }

    [Test]
    public void Handle_QuandoEstabelecimentoNaoEncontrado_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterCpfENomeFantasiaAsync(EstabelecimentoId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(((string?)null, (string?)null));

        var cmd = new ResetTenantCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            AdminId = AdminId,
            ConfirmarNomeFantasia = "Qualquer nome",
            Motivo = "Motivo de reset válido",
        };

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
    }

    [Test]
    public async Task Handle_NomeCaseInsensitive_ExecutaReset()
    {
        _repo.Setup(r => r.ObterCpfENomeFantasiaAsync(EstabelecimentoId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(("12345678900", "Clínica Feliz"));

        _audit.Setup(a => a.RegistrarAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                                            It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<string?>(),
                                            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        var cmd = new ResetTenantCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            AdminId = AdminId,
            ConfirmarNomeFantasia = "CLÍNICA FELIZ", // maiúsculas
            Motivo = "Motivo de reset válido",
        };

        await _sut.Handle(cmd);

        _resetService.Verify(r => r.ResetEstabelecimentoAsync(
            EstabelecimentoId, It.IsAny<ResetModulos>(), "Motivo de reset válido", AdminId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
