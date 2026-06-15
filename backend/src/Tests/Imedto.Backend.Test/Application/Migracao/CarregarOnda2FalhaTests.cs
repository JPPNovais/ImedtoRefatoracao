using Imedto.Backend.Application.Migracao.Jobs;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Migracao;

/// <summary>
/// CA26 — addendum 002: CarregarOnda2JobHandler marca falhou ao invés de re-lançar.
/// CA27 — espera legítima da Onda 1 (bloqueada) NÃO vira falha.
///
/// Nota: os handlers de prontuário internos (IniciarProntuarioCommandHandler etc.)
/// são criados via NullObject implícito — o teste valida o comportamento de falha
/// que ocorre antes de chegar a eles (via ExisteOnda1Ativa e ListarPorJob).
/// </summary>
[TestFixture]
public class CarregarOnda2FalhaTests
{
    private Mock<IMigracaoJobRepository> _jobRepo;
    private Mock<IMigracaoRegistroRepository> _registroRepo;
    private Mock<IMigracaoPacienteLookup> _pacienteLookup;
    private Mock<IProntuarioRepository> _prontuarioRepo;

    private static readonly Guid AdminId = Guid.NewGuid();
    private const long EstId = 42L;
    private const long JobId = 66L;

    [SetUp]
    public void SetUp()
    {
        _jobRepo        = new Mock<IMigracaoJobRepository>();
        _registroRepo   = new Mock<IMigracaoRegistroRepository>();
        _pacienteLookup = new Mock<IMigracaoPacienteLookup>();
        _prontuarioRepo = new Mock<IProntuarioRepository>();
    }

    /// <summary>
    /// Cria o handler com dependências mínimas suficientes para os testes de falha.
    /// As dependências de prontuário nunca são atingidas nos cenários de falha precoce.
    /// </summary>
    private CarregarOnda2JobHandler CriarSut()
    {
        // Os handlers de prontuário são sealed sem interface — não podem ser mockados.
        // Nos testes de falha, a execução nunca chega a eles:
        // - CA27: retorna cedo antes de listar registros (Onda 1 ativa).
        // - CA26: lança antes de chamar handlers de prontuário (ListarPorJob falha).
        // Passamos null — seguro porque o caminho de teste não os invoca.
        return new CarregarOnda2JobHandler(
            _jobRepo.Object,
            _registroRepo.Object,
            _pacienteLookup.Object,
            _prontuarioRepo.Object,
            null!,  // IniciarProntuarioCommandHandler — não atingido nos testes
            null!,  // RegistrarEvolucaoCommandHandler — não atingido nos testes
            null!,  // AdicionarAnexoCommandHandler — não atingido nos testes
            NullLogger<CarregarOnda2JobHandler>.Instance);
    }

    private MigracaoJob CriarJobMigrandoOnda2()
    {
        var job = MigracaoJob.Criar(EstId, AdminId, onda: MigracaoJob.OndaProntuario);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        // Addendum 003: upload → aguardando_aprovacao; AprovarAnalise → aguardando_mapa.
        job.RegistrarArquivoRecebido("migracao/42/66/prontuario.zip");
        job.AprovarAnalise(Guid.NewGuid());
        job.MarcarMapaEmRevisao();
        job.MarcarPreviewPronto(AdminId);
        job.MarcarMigrando(AdminId);
        return job;
    }

    /// <summary>
    /// CA27 — quando a Onda 1 ainda está ativa, o job de Onda 2 NÃO vai para falhou.
    /// O status permanece "migrando" e o job é re-tentado na próxima rodada do scheduler.
    /// </summary>
    [Test]
    public async Task ExecutarAsync_Onda1Ativa_NaoMarcaFalhou()
    {
        var job = CriarJobMigrandoOnda2();
        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOnda2OuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        // CA13 — Onda 1 ainda ativa para o tenant.
        _jobRepo.Setup(r => r.ExisteOnda1AtivaParaTenant(EstId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CriarSut();
        await sut.ExecutarAsync(CancellationToken.None);

        // Job permanece em "migrando" (espera legítima) — NÃO deve ir para "falhou".
        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusMigrando));
        Assert.That(job.MotivoFalha, Is.Null);

        // Salvar NÃO deve ser chamado (job não mudou de status).
        _jobRepo.Verify(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// CA26 — quando ListarPorJob lança exceção inesperada (Onda 1 já concluída),
    /// o job deve ser marcado como falhou em vez de re-lançar.
    /// </summary>
    [Test]
    public async Task ExecutarAsync_ExcecaoAposOnda1Concluida_MarcaFalhou()
    {
        var job = CriarJobMigrandoOnda2();
        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOnda2OuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        // Onda 1 concluída — pode prosseguir.
        _jobRepo.Setup(r => r.ExisteOnda1AtivaParaTenant(EstId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Falha inesperada ao listar registros.
        _registroRepo.Setup(r => r.ListarPorJob(JobId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("falha de banco"));

        MigracaoJob? jobSalvo = null;
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Callback<MigracaoJob, CancellationToken>((j, _) => jobSalvo = j)
            .Returns(Task.CompletedTask);

        var sut = CriarSut();

        // Não deve propagar a exceção (antes do addendum, propagava e travava o job).
        Assert.DoesNotThrowAsync(() => sut.ExecutarAsync(CancellationToken.None));

        Assert.That(jobSalvo, Is.Not.Null);
        Assert.That(jobSalvo!.Status, Is.EqualTo(MigracaoJob.StatusFalhou));
        Assert.That(jobSalvo.MotivoFalha, Is.EqualTo("falha inesperada na carga"));
        Assert.That(jobSalvo.StatusAntesFalha, Is.EqualTo(MigracaoJob.StatusMigrando));
    }
}
