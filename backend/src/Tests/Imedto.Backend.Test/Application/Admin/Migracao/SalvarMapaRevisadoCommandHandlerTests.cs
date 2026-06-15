using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Admin.Migracao;

[TestFixture]
public class SalvarMapaRevisadoCommandHandlerTests
{
    private Mock<IMigracaoMapaRepository> _mapaRepo;
    private SalvarMapaRevisadoCommandHandler _sut;
    private static readonly Guid AdminId = Guid.NewGuid();
    private const long JobId = 10;
    private const string Entidade = "paciente";

    [SetUp]
    public void SetUp()
    {
        _mapaRepo = new Mock<IMigracaoMapaRepository>();
        _sut = new SalvarMapaRevisadoCommandHandler(_mapaRepo.Object);
    }

    [Test]
    public async Task Handle_MapaExistente_RevisaESalva()
    {
        var mapa = MigracaoMapa.Criar(JobId, 42, Entidade, "{\"de_para\":{},\"confianca\":0.5,\"duvidas\":[]}");

        _mapaRepo.Setup(r => r.ObterPorJobEEntidadeAdminOuNulo(JobId, Entidade, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapa);

        var cmd = new SalvarMapaRevisadoCommand
        {
            JobId            = JobId,
            Entidade         = Entidade,
            DePara           = new Dictionary<string, string> { ["nome"] = "nome", ["cpf"] = "cpf" },
            RevisadoPorUsuarioId = AdminId,
        };

        await _sut.Handle(cmd);

        _mapaRepo.Verify(r => r.Salvar(mapa, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(mapa.RevisadoPorUsuarioId, Is.EqualTo(AdminId));
        Assert.That(mapa.RevisadoEm, Is.Not.Null);
    }

    [Test]
    public void Handle_MapaNaoEncontrado_LancaBusinessException()
    {
        _mapaRepo.Setup(r => r.ObterPorJobEEntidadeAdminOuNulo(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MigracaoMapa?)null);

        var cmd = new SalvarMapaRevisadoCommand
        {
            JobId = JobId, Entidade = Entidade,
            DePara = [], RevisadoPorUsuarioId = AdminId,
        };

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("não encontrado"));
    }

    [Test]
    public void Handle_AdminIdVazio_LancaBusinessException()
    {
        var cmd = new SalvarMapaRevisadoCommand
        {
            JobId = JobId, Entidade = Entidade,
            DePara = [], RevisadoPorUsuarioId = Guid.Empty,
        };

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
    }
}
