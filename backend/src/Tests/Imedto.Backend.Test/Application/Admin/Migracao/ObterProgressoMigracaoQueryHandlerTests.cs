using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Admin.Migracao;

[TestFixture]
public class ObterProgressoMigracaoQueryHandlerTests
{
    [Test]
    public async Task Handle_RepassaJobIdAoRepositorio()
    {
        var mockRepo = new Mock<MigracaoAdminQueryRepository>(new AppReadConnectionString("Host=fake")) { CallBase = false };
        var esperado = new ProgressoMigracaoResult
        {
            PorEntidade = new Dictionary<string, ProgressoEntidadeDto>
            {
                ["paciente"] = new() { Total = 100, Pendentes = 20, Criados = 70, Atualizados = 10, Percentual = 80 },
            },
            PercentualAgregado = 80,
        };

        mockRepo
            .Setup(r => r.ObterProgressoAsync(7L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(esperado);

        var sut = new ObterProgressoMigracaoQueryHandler(mockRepo.Object);
        var result = await sut.Handle(7L);

        Assert.That(result.PercentualAgregado, Is.EqualTo(80));
        Assert.That(result.PorEntidade, Contains.Key("paciente"));
        Assert.That(result.PorEntidade["paciente"].Total, Is.EqualTo(100));
        mockRepo.Verify(r => r.ObterProgressoAsync(7L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_RetornaProgressoVazioParaJobSemRegistros()
    {
        var mockRepo = new Mock<MigracaoAdminQueryRepository>(new AppReadConnectionString("Host=fake")) { CallBase = false };
        mockRepo
            .Setup(r => r.ObterProgressoAsync(0L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProgressoMigracaoResult { PercentualAgregado = 0 });

        var sut = new ObterProgressoMigracaoQueryHandler(mockRepo.Object);
        var result = await sut.Handle(0L);

        Assert.That(result.PercentualAgregado, Is.EqualTo(0));
        Assert.That(result.PorEntidade, Is.Empty);
    }
}
