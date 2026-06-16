using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Admin.Migracao;

/// <summary>
/// Verifica que AprovarAnaliseCommandHandler grava evento de transição após aprovação
/// (addendum 003 — CA51/CA52/CA53).
/// </summary>
[TestFixture]
public class AprovarAnaliseGravaEventoTests
{
    private Mock<IMigracaoJobRepository>        _jobRepo;
    private Mock<IMigracaoJobEventoRepository>  _eventoRepo;
    private AprovarAnaliseCommandHandler        _sut;

    private const long JobId             = 1L;
    private const long EstabelecimentoId = 42L;

    [SetUp]
    public void SetUp()
    {
        _jobRepo    = new Mock<IMigracaoJobRepository>();
        _eventoRepo = new Mock<IMigracaoJobEventoRepository>();

        _eventoRepo.Setup(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        _sut = new AprovarAnaliseCommandHandler(_jobRepo.Object, _eventoRepo.Object);
    }

    [Test]
    public async Task Handle_GravaEventoComStatusCorretos()
    {
        var adminId = Guid.NewGuid();
        var job = CriarJobEmAguardandoAprovacao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

        await _sut.Handle(new AprovarAnaliseCommand { JobId = JobId, AdminId = adminId });

        _eventoRepo.Verify(r => r.Gravar(
            It.Is<MigracaoJobEvento>(e =>
                e.StatusAnterior == MigracaoJob.StatusAguardandoAprovacao &&
                e.StatusNovo     == MigracaoJob.StatusAguardandoMapa &&
                e.UsuarioId      == adminId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_GravaEventoAposJobSalvo()
    {
        // Confirma que o evento é persistido mesmo que SaveChanges do job não falhe.
        var adminId = Guid.NewGuid();
        var job = CriarJobEmAguardandoAprovacao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

        var ordemChamadas = new List<string>();
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
                .Callback(() => ordemChamadas.Add("salvar"))
                .Returns(Task.CompletedTask);
        _eventoRepo.Setup(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()))
                   .Callback(() => ordemChamadas.Add("gravar"))
                   .Returns(Task.CompletedTask);

        await _sut.Handle(new AprovarAnaliseCommand { JobId = JobId, AdminId = adminId });

        Assert.That(ordemChamadas, Is.EqualTo(new[] { "salvar", "gravar" }),
            "Evento deve ser gravado DEPOIS de Salvar o job.");
    }

    [Test]
    public void Handle_JobNaoEncontrado_NaoGravaEvento()
    {
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((MigracaoJob?)null);

        Assert.ThrowsAsync<BusinessException>(() =>
            _sut.Handle(new AprovarAnaliseCommand { JobId = JobId, AdminId = Guid.NewGuid() }));

        _eventoRepo.Verify(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Handle_JobEmStatusErrado_NaoGravaEvento()
    {
        // Job em aguardando_mapa não pode ser aprovado novamente.
        var job = CriarJobEmAguardandoAprovacao();
        job.AprovarAnalise(Guid.NewGuid()); // agora está em aguardando_mapa

        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

        Assert.ThrowsAsync<BusinessException>(() =>
            _sut.Handle(new AprovarAnaliseCommand { JobId = JobId, AdminId = Guid.NewGuid() }));

        _eventoRepo.Verify(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private static MigracaoJob CriarJobEmAguardandoAprovacao()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, Guid.NewGuid());
        // Simular ID persistido para que MigracaoJobEvento.Criar não rejeite migracaoJobId <= 0
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        job.RegistrarArquivoRecebido("s3://test/key.zip");
        // Após RegistrarArquivoRecebido → status = aguardando_aprovacao
        return job;
    }
}
