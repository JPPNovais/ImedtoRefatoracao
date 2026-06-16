using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Admin.Migracao;

[TestFixture]
public class ObterEventosMigracaoQueryHandlerTests
{
    [Test]
    public async Task Handle_RepassaJobIdAoRepositorio()
    {
        var mockRepo = new Mock<MigracaoAdminQueryRepository>(new AppReadConnectionString("Host=fake")) { CallBase = false };
        mockRepo
            .Setup(r => r.ListarEventosAsync(42L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MigracaoJobEventoDto>
            {
                new() { StatusNovo = "aguardando_mapa", CriadoEm = DateTime.UtcNow }
            });

        var sut = new ObterEventosMigracaoQueryHandler(mockRepo.Object);
        var result = await sut.Handle(42L);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].StatusNovo, Is.EqualTo("aguardando_mapa"));
        mockRepo.Verify(r => r.ListarEventosAsync(42L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_RetornaListaVaziaQuandoSemEventos()
    {
        var mockRepo = new Mock<MigracaoAdminQueryRepository>(new AppReadConnectionString("Host=fake")) { CallBase = false };
        mockRepo
            .Setup(r => r.ListarEventosAsync(99L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MigracaoJobEventoDto>());

        var sut = new ObterEventosMigracaoQueryHandler(mockRepo.Object);
        var result = await sut.Handle(99L);

        Assert.That(result, Is.Empty);
    }
}
