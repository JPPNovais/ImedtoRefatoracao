using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.Infrastructure.Migracao;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Migracao;

/// <summary>
/// Testes do job de expiração de arquivos ZIP de migração (CA24, R12 — briefing 2026-06-15_001 Marco 1).
/// </summary>
[TestFixture]
public class ExpirarArquivosMigracaoJobTests
{
    private Mock<IMigracaoJobRepository> _repo;
    private Mock<IMigracaoArquivoStorageService> _storage;
    private ExpirarArquivosMigracaoJob _sut;

    [SetUp]
    public void SetUp()
    {
        _repo    = new Mock<IMigracaoJobRepository>();
        _storage = new Mock<IMigracaoArquivoStorageService>();
        _storage.Setup(s => s.RemoverArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        _sut = new ExpirarArquivosMigracaoJob(_repo.Object, _storage.Object,
            NullLogger<ExpirarArquivosMigracaoJob>.Instance);
    }

    private static MigracaoJob JobComArquivoExpirado()
    {
        var job = MigracaoJob.Criar(estabelecimentoId: 1, criadoPorUsuarioId: Guid.NewGuid());
        // Simula arquivo recebido no passado.
        job.RegistrarArquivoRecebido("migracao/1/1/arquivo.zip");
        return job;
    }

    // ─── CA24 — Expiração de arquivo ────────────────────────────────────────────

    [Test]
    public async Task Executar_ComJobElegivel_RemoveArquivoEMarcaExpirado()
    {
        var job = JobComArquivoExpirado();
        _repo.Setup(r => r.ListarComArquivoParaExpirar(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<MigracaoJob> { job });

        await _sut.ExecutarAsync(CancellationToken.None);

        // Arquivo removido do S3.
        _storage.Verify(s => s.RemoverArquivoAsync("migracao/1/1/arquivo.zip", It.IsAny<CancellationToken>()),
            Times.Once);

        // Job marcado como expirado e persistido.
        Assert.That(job.ArquivoExpirado, Is.True);
        _repo.Verify(r => r.Salvar(job, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Executar_SemJobsElegiveis_NaoFazI_O()
    {
        _repo.Setup(r => r.ListarComArquivoParaExpirar(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<MigracaoJob>());

        await _sut.ExecutarAsync(CancellationToken.None);

        _storage.Verify(s => s.RemoverArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repo.Verify(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Falha em um job individual não deve derrubar os demais (resiliência).</summary>
    [Test]
    public async Task Executar_FalhaEmUmJob_ContinuaProcessandoOsOutros()
    {
        var jobBom  = JobComArquivoExpirado();
        var jobRuim = JobComArquivoExpirado();

        _repo.Setup(r => r.ListarComArquivoParaExpirar(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<MigracaoJob> { jobRuim, jobBom });

        // Primeiro job lança — segundo deve continuar.
        var vez = 0;
        _storage.Setup(s => s.RemoverArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    if (vez++ == 0) throw new InvalidOperationException("S3 timeout simulado");
                    return Task.CompletedTask;
                });

        // Não deve lançar ao chamador.
        Assert.DoesNotThrowAsync(() => _sut.ExecutarAsync(CancellationToken.None));

        // jobBom processado apesar da falha anterior.
        Assert.That(jobBom.ArquivoExpirado, Is.True);
        _repo.Verify(r => r.Salvar(jobBom, It.IsAny<CancellationToken>()), Times.Once);
        // jobRuim não foi marcado como expirado (S3 falhou).
        Assert.That(jobRuim.ArquivoExpirado, Is.False);
    }

    // ─── Nome do job (registro no JobScheduler) ──────────────────────────────────

    [Test]
    public void Nome_EhExpirarArquivosMigracao()
    {
        Assert.That(_sut.Nome, Is.EqualTo("expirar-arquivos-migracao"));
    }
}
