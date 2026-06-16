using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Migracao;

/// <summary>
/// Testes do DisparaMigracaoCommandHandler (briefing 2026-06-15_001 — Marco 3).
/// Cobrem: CA22 (retorna imediatamente após marcar migrando), multi-tenant, validações de input.
/// </summary>
[TestFixture]
public class DisparaMigracaoCommandHandlerTests
{
    private Mock<IMigracaoJobRepository>       _jobRepo;
    private Mock<IMigracaoJobEventoRepository> _eventoRepo;
    private DisparaMigracaoCommandHandler      _sut;

    private static readonly Guid AdminId = Guid.NewGuid();
    private const long JobId = 77;
    private const long EstabelecimentoId = 42;

    [SetUp]
    public void SetUp()
    {
        _jobRepo    = new Mock<IMigracaoJobRepository>();
        _eventoRepo = new Mock<IMigracaoJobEventoRepository>();
        _eventoRepo.Setup(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);
        _sut = new DisparaMigracaoCommandHandler(_jobRepo.Object, _eventoRepo.Object);
    }

    private static MigracaoJob JobNaFase(string status)
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, Guid.NewGuid(), "iClinic");
        // Simula ID persistido para que MigracaoJobEvento.Criar não rejeite migracaoJobId <= 0
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        if (status == "preview_pronto" || status == "migrando")
        {
            // Addendum 003: upload → aguardando_aprovacao; AprovarAnalise → aguardando_mapa.
            job.RegistrarArquivoRecebido("migracao/42/77/arquivo.zip");
            job.AprovarAnalise(AdminId);
            job.MarcarMapaEmRevisao();
            job.MarcarPreviewPronto(AdminId);
        }
        return job;
    }

    [Test]
    public async Task Handle_QuandoJobEmPreviewPronto_MarcaMigrandoESalva()
    {
        // Arrange
        var job = JobNaFase("preview_pronto");
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);

        var cmd = new DisparaMigracaoCommand { JobId = JobId, AdminId = AdminId };

        // Act
        await _sut.Handle(cmd);

        // Assert
        Assert.That(job.Status, Is.EqualTo("migrando"), "Job deve estar em 'migrando' após disparo.");
        _jobRepo.Verify(r => r.Salvar(job, default), Times.Once, "Deve salvar o job com novo status.");
    }

    [Test]
    public void Handle_QuandoJobNaoEncontrado_LancaBusinessException()
    {
        // Arrange
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(It.IsAny<long>(), default)).ReturnsAsync((MigracaoJob?)null);

        var cmd = new DisparaMigracaoCommand { JobId = JobId, AdminId = AdminId };

        // Act & Assert
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
    }

    [Test]
    public void Handle_QuandoJobIdInvalido_LancaBusinessException()
    {
        // Arrange
        var cmd = new DisparaMigracaoCommand { JobId = 0, AdminId = AdminId };

        // Act & Assert
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
    }

    [Test]
    public void Handle_QuandoAdminIdVazio_LancaBusinessException()
    {
        // Arrange
        var cmd = new DisparaMigracaoCommand { JobId = JobId, AdminId = Guid.Empty };

        // Act & Assert
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
    }

    [Test]
    public void Handle_QuandoJobNaoEstaEmPreviewPronto_LancaBusinessException()
    {
        // Arrange — Job em mapa_em_revisao, não em preview_pronto
        var job = MigracaoJob.Criar(EstabelecimentoId, Guid.NewGuid(), "iClinic");
        // Addendum 003: precisa aprovar antes de marcar mapa em revisão.
        job.RegistrarArquivoRecebido("migracao/42/77/arquivo.zip");
        job.AprovarAnalise(AdminId);
        job.MarcarMapaEmRevisao();
        // Não chama MarcarPreviewPronto → status = "mapa_em_revisao"

        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);

        var cmd = new DisparaMigracaoCommand { JobId = JobId, AdminId = AdminId };

        // Act & Assert
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd),
            "Transição inválida: mapa_em_revisao → migrando deve ser recusada.");
    }
}
