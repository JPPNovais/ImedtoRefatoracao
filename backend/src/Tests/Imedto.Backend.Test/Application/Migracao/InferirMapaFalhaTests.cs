using Imedto.Backend.Application.Migracao.Jobs;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Migracao;

/// <summary>
/// CA25 — addendum 002: InferirMapaMigracaoJobHandler marca falhou ao invés de engolir silenciosamente.
/// </summary>
[TestFixture]
public class InferirMapaFalhaTests
{
    private Mock<IMigracaoJobRepository> _jobRepo;
    private Mock<IMigracaoArquivoStorageService> _storage;
    private Mock<IMapeadorDeMigracao> _mapeador;
    private Mock<IMigracaoMapaRepository> _mapaRepo;
    private Mock<IMigracaoTemplateRepository> _templateRepo;

    private static readonly Guid UsuarioId = Guid.NewGuid();
    private const long EstabelecimentoId = 42L;
    private const long JobId = 7L;

    [SetUp]
    public void SetUp()
    {
        _jobRepo      = new Mock<IMigracaoJobRepository>();
        _storage      = new Mock<IMigracaoArquivoStorageService>();
        _mapeador     = new Mock<IMapeadorDeMigracao>();
        _mapaRepo     = new Mock<IMigracaoMapaRepository>();
        _templateRepo = new Mock<IMigracaoTemplateRepository>();

        _templateRepo.Setup(r => r.ListarPorNome(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private InferirMapaMigracaoJobHandler CriarSut()
    {
        return new InferirMapaMigracaoJobHandler(
            _jobRepo.Object,
            _storage.Object,
            _mapeador.Object,
            _mapaRepo.Object,
            _templateRepo.Object,
            [],
            NullLogger<InferirMapaMigracaoJobHandler>.Instance);
    }

    private MigracaoJob CriarJobAguardandoMapa()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        job.RegistrarArquivoRecebido("migracao/42/7/arquivo.zip");
        return job;
    }

    /// <summary>
    /// CA25 — quando o download do S3 falha, o job deve ser marcado como "falhou"
    /// com motivo categórico, e o Salvar deve ser chamado.
    /// </summary>
    [Test]
    public async Task ExecutarAsync_S3Falha_MarcaJobComoFalhou()
    {
        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("S3 NoSuchKey"));

        MigracaoJob? jobSalvo = null;
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Callback<MigracaoJob, CancellationToken>((j, _) => jobSalvo = j)
            .Returns(Task.CompletedTask);

        var sut = CriarSut();
        await sut.ExecutarAsync(CancellationToken.None);

        Assert.That(jobSalvo, Is.Not.Null, "Job deve ser salvo após falha.");
        Assert.That(jobSalvo!.Status, Is.EqualTo(MigracaoJob.StatusFalhou));
        Assert.That(jobSalvo.StatusAntesFalha, Is.EqualTo(MigracaoJob.StatusAguardandoMapa));
        Assert.That(jobSalvo.MotivoFalha, Is.Not.Null.And.Not.Empty);
    }

    /// <summary>
    /// CA25 — job em falhou NÃO é re-selecionado pelo recorrente (pois ObterMaisAntigoAguardandoMapaOuNulo
    /// filtra por status aguardando_mapa — o recorrente não buscará o job em falhou).
    /// Este teste verifica que o job saiu do estado aguardando_mapa após a falha.
    /// </summary>
    [Test]
    public async Task ExecutarAsync_FalhaInferencia_JobSaiDeAguardandoMapa()
    {
        var job = CriarJobAguardandoMapa();
        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusAguardandoMapa));

        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("arquivo ZIP corrompido"));
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CriarSut();
        await sut.ExecutarAsync(CancellationToken.None);

        // Job deve estar em "falhou" — não mais em "aguardando_mapa".
        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusFalhou));
        // Motivo: ZIP corrompido → categoria "arquivo corrompido ou ilegível".
        Assert.That(job.MotivoFalha, Is.EqualTo("arquivo corrompido ou ilegível"));
    }

    /// <summary>
    /// CA28 — motivo da falha é categoria legível sem PII (não a mensagem técnica crua).
    /// </summary>
    [Test]
    public async Task ExecutarAsync_FalhaIa_MotivoCategoriaGenerica()
    {
        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        // Simula HTTP 401 — credencial de IA ausente/inválida.
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Net.Http.HttpRequestException(
                "Unauthorized — ApiKey inválida",
                null,
                System.Net.HttpStatusCode.Unauthorized));

        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CriarSut();
        await sut.ExecutarAsync(CancellationToken.None);

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusFalhou));
        // Categoria genérica — não deve vazar mensagem crua da exceção.
        Assert.That(job.MotivoFalha, Is.EqualTo("IA não configurada"));
        Assert.That(job.MotivoFalha, Does.Not.Contain("Unauthorized"));
        Assert.That(job.MotivoFalha, Does.Not.Contain("ApiKey"));
    }
}
