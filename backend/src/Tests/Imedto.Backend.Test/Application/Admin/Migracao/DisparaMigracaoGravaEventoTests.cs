using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Admin.Migracao;

/// <summary>
/// Verifica que DisparaMigracaoCommandHandler grava evento de transição
/// preview_pronto → migrando (addendum 003 — CA51).
/// </summary>
[TestFixture]
public class DisparaMigracaoGravaEventoTests
{
    private Mock<IMigracaoJobRepository>       _jobRepo;
    private Mock<IMigracaoJobEventoRepository> _eventoRepo;
    private DisparaMigracaoCommandHandler      _sut;

    private const long JobId             = 5L;
    private const long EstabelecimentoId = 42L;

    [SetUp]
    public void SetUp()
    {
        _jobRepo    = new Mock<IMigracaoJobRepository>();
        _eventoRepo = new Mock<IMigracaoJobEventoRepository>();

        _eventoRepo.Setup(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        _sut = new DisparaMigracaoCommandHandler(_jobRepo.Object, _eventoRepo.Object);
    }

    [Test]
    public async Task Handle_GravaEventoPreviewProntoParaMigrando()
    {
        var adminId = Guid.NewGuid();
        var job = CriarJobEmPreviewPronto(adminId);
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

        await _sut.Handle(new DisparaMigracaoCommand { JobId = JobId, AdminId = adminId });

        _eventoRepo.Verify(r => r.Gravar(
            It.Is<MigracaoJobEvento>(e =>
                e.StatusAnterior == MigracaoJob.StatusPreviewPronto &&
                e.StatusNovo     == MigracaoJob.StatusMigrando &&
                e.UsuarioId      == adminId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Handle_JobNaoEncontrado_NaoGravaEvento()
    {
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((MigracaoJob?)null);

        Assert.ThrowsAsync<BusinessException>(() =>
            _sut.Handle(new DisparaMigracaoCommand { JobId = JobId, AdminId = Guid.NewGuid() }));

        _eventoRepo.Verify(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private static MigracaoJob CriarJobEmPreviewPronto(Guid adminId)
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, Guid.NewGuid());
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        job.RegistrarArquivoRecebido("s3://test/key.zip");
        job.AprovarAnalise(adminId);
        job.MarcarMapaEmRevisao();
        job.MarcarPreviewPronto(adminId);
        return job;
    }
}
