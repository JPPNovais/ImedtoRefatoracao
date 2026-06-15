using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Migracao;

/// <summary>
/// CA30/CA31 — addendum 002: ReprocessarMigracaoCommandHandler.
/// </summary>
[TestFixture]
public class ReprocessarMigracaoCommandHandlerTests
{
    private Mock<IMigracaoJobRepository> _jobRepo;
    private static readonly Guid AdminId = Guid.NewGuid();
    private const long EstId = 42L;
    private const long JobId = 77L;

    [SetUp]
    public void SetUp()
    {
        _jobRepo = new Mock<IMigracaoJobRepository>();
    }

    private ReprocessarMigracaoCommandHandler CriarSut() => new(_jobRepo.Object);

    private MigracaoJob CriarJobFalhou(string statusAntes = MigracaoJob.StatusAguardandoMapa)
    {
        var job = statusAntes == MigracaoJob.StatusMigrando
            ? CriarJobMigrando()
            : CriarJobAguardandoMapa();

        job.MarcarFalhou("IA não configurada");
        return job;
    }

    private static MigracaoJob CriarJobAguardandoMapa()
    {
        var job = MigracaoJob.Criar(EstId, AdminId);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        // Addendum 003: upload → aguardando_aprovacao; AprovarAnalise → aguardando_mapa.
        job.RegistrarArquivoRecebido("migracao/42/77/arquivo.zip");
        job.AprovarAnalise(Guid.NewGuid());
        return job;
    }

    private static MigracaoJob CriarJobMigrando()
    {
        var job = CriarJobAguardandoMapa();
        job.MarcarMapaEmRevisao();
        job.MarcarPreviewPronto(AdminId);
        job.MarcarMigrando(AdminId);
        return job;
    }

    /// <summary>CA30 — Reprocessar job que falhou na inferência restaura aguardando_mapa.</summary>
    [Test]
    public async Task Handle_FalhaInferencia_RestaurAguardandoMapa()
    {
        var job = CriarJobFalhou(MigracaoJob.StatusAguardandoMapa);
        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusFalhou));

        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CriarSut();
        await sut.Handle(JobId);

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusAguardandoMapa));
        Assert.That(job.MotivoFalha, Is.Null);

        _jobRepo.Verify(r => r.Salvar(job, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>CA30 — Reprocessar job que falhou na carga restaura migrando.</summary>
    [Test]
    public async Task Handle_FalhaCarga_RestaurarMigrando()
    {
        var job = CriarJobFalhou(MigracaoJob.StatusMigrando);
        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusFalhou));

        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CriarSut();
        await sut.Handle(JobId);

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusMigrando));
    }

    /// <summary>CA31 — Reprocessar job que NÃO está em falhou lança 422 (BusinessException).</summary>
    [Test]
    public async Task Handle_JobNaoFalhou_LancaBusinessException()
    {
        var job = CriarJobAguardandoMapa();
        // status = aguardando_mapa (não falhou)

        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var sut = CriarSut();

        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(JobId));
        Assert.That(ex!.Message, Does.Contain("falharam"));

        // Salvar NÃO deve ser chamado.
        _jobRepo.Verify(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>CA31 — Job não encontrado lança BusinessException.</summary>
    [Test]
    public async Task Handle_JobNaoEncontrado_LancaBusinessException()
    {
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MigracaoJob?)null);

        var sut = CriarSut();

        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(JobId));
        Assert.That(ex!.Message, Does.Contain("não encontrado").IgnoreCase);
    }

    /// <summary>CA31 — JobId inválido (0) lança BusinessException sem ir ao banco.</summary>
    [Test]
    public async Task Handle_JobIdZero_LancaBusinessException()
    {
        var sut = CriarSut();

        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(0));
        Assert.That(ex!.Message, Does.Contain("inválido").IgnoreCase);

        _jobRepo.Verify(r => r.ObterPorIdAdminOuNulo(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
