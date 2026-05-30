using Imedto.Backend.Application.Admin.Estabelecimentos.Queries;
using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Admin.Estabelecimentos;

[TestFixture]
public class RevelarCpfDonoQueryHandlerTests
{
    private Mock<IAdminEstabelecimentosQueryRepository> _repo;
    private Mock<ImedtoAdminAuditWriter> _audit;
    private RevelarCpfDonoQueryHandler _sut;

    private static readonly Guid AdminId = Guid.NewGuid();
    private const long EstabelecimentoId = 10L;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IAdminEstabelecimentosQueryRepository>();
        _audit = new Mock<ImedtoAdminAuditWriter>(null!, null!, null!);
        _sut = new RevelarCpfDonoQueryHandler(_repo.Object, _audit.Object);
    }

    [Test]
    public async Task Handle_QuandoCpfExiste_RetornaCpfFormatadoEGravaAudit()
    {
        _repo.Setup(r => r.ObterCpfENomeFantasiaAsync(EstabelecimentoId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(("12345678900", "Clínica A"));

        _audit.Setup(a => a.RegistrarAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                                            It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<string?>(),
                                            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        var query = new RevelarCpfDonoQuery
        {
            EstabelecimentoId = EstabelecimentoId,
            AdminId = AdminId,
            Motivo = "Verificar dados do dono",
        };

        var resultado = await _sut.Handle(query);

        Assert.That(resultado.Cpf, Is.EqualTo("123.456.789-00"));

        // Verifica que audit foi registrado com ação correta.
        _audit.Verify(a => a.RegistrarAsync(
            "REVELAR_CPF_DONO",
            AdminId,
            "Estabelecimento",
            EstabelecimentoId.ToString(),
            EstabelecimentoId,
            "Verificar dados do dono",
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Handle_QuandoMotivoMenorQue10Chars_LancaBusinessException()
    {
        var query = new RevelarCpfDonoQuery
        {
            EstabelecimentoId = EstabelecimentoId,
            AdminId = AdminId,
            Motivo = "curto",
        };

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(query));
        _repo.Verify(r => r.ObterCpfENomeFantasiaAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Handle_QuandoEstabelecimentoNaoEncontrado_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterCpfENomeFantasiaAsync(EstabelecimentoId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(((string?)null, (string?)null));

        var query = new RevelarCpfDonoQuery
        {
            EstabelecimentoId = EstabelecimentoId,
            AdminId = AdminId,
            Motivo = "Motivo com dez chars",
        };

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(query));
    }
}
