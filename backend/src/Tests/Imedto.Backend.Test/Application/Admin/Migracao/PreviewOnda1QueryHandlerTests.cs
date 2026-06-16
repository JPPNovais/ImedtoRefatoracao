using System.Text.Json;
using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Admin.Migracao;

/// <summary>
/// Testes do PreviewOnda1QueryHandler após addendum 6 (CA104/CA116).
/// Verifica que a materialização é chamada antes de contar os registros
/// e que TotalRegistros reflete os registros reais (não zero).
/// </summary>
[TestFixture]
public class PreviewOnda1QueryHandlerTests
{
    private Mock<IMigracaoJobRepository>          _jobRepo;
    private Mock<IMigracaoRegistroRepository>     _registroRepo;
    private Mock<IMigracaoJobEventoRepository>    _eventoRepo;
    private Mock<IMigracaoMapaRepository>         _mapaRepo;
    private Mock<IMigracaoArquivoStorageService>  _storage;
    private Mock<IMigracaoArquivoParser>          _parser;

    private const long EstabelecimentoId = 42;
    private const long JobId             = 99;
    private static readonly Guid AdminId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _jobRepo      = new Mock<IMigracaoJobRepository>();
        _mapaRepo     = new Mock<IMigracaoMapaRepository>();
        _registroRepo = new Mock<IMigracaoRegistroRepository>();
        _storage      = new Mock<IMigracaoArquivoStorageService>();
        _parser       = new Mock<IMigracaoArquivoParser>();
        _eventoRepo   = new Mock<IMigracaoJobEventoRepository>();

        _eventoRepo.Setup(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);
    }

    private MigracaoJob CriarJobEmRevisao()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, Guid.NewGuid(), "iClinic");
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        job.RegistrarArquivoRecebido("migracao/42/99/arquivo.zip");
        job.AprovarAnalise(AdminId);
        job.MarcarMapaEmRevisao();
        return job;
    }

    private PreviewOnda1QueryHandler CriarSut()
    {
        var materializarHandler = new MaterializarRegistrosCommandHandler(
            _jobRepo.Object,
            _mapaRepo.Object,
            _registroRepo.Object,
            _storage.Object,
            new[] { _parser.Object },
            NullLogger<MaterializarRegistrosCommandHandler>.Instance);

        return new PreviewOnda1QueryHandler(
            _jobRepo.Object,
            _registroRepo.Object,
            _eventoRepo.Object,
            materializarHandler);
    }

    // ─── CA104 — preview conta o real, não zero ──────────────────────────────────

    [Test]
    public async Task CA104_PreviewContaRegistrosMaterializados_NaoZero()
    {
        // Arrange
        var job = CriarJobEmRevisao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);

        // Sem mapas (materialização não cria nada — mas lista de registros JÁ tem algo)
        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, default))
                 .ReturnsAsync(new List<MigracaoMapa>());
        _registroRepo.Setup(r => r.DeletarPendentesPorJob(JobId, default)).Returns(Task.CompletedTask);

        // Simula que APÓS a materialização (que pode ter sido feita antes) há 30 pendentes
        var registros = Enumerable.Range(1, 30)
            .Select(i =>
            {
                var r = MigracaoRegistro.Criar(JobId, EstabelecimentoId, "paciente", $"{{\"id\":{i}}}");
                typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(r, (long)i);
                return r;
            })
            .ToList();

        _registroRepo.Setup(r => r.ListarPorJob(JobId, default)).ReturnsAsync(registros);

        var sut = CriarSut();

        // Act
        var resultado = await sut.Handle(JobId, AdminId, default);

        // Assert
        Assert.That(resultado.TotalRegistros, Is.EqualTo(30), "CA104: TotalRegistros deve ser 30 (real), não 0");
        Assert.That(resultado.PorEntidade.ContainsKey("paciente"), Is.True);
        Assert.That(resultado.PorEntidade["paciente"].Pendentes, Is.EqualTo(30));
    }

    // ─── CA104 — preview chama materialização antes de contar ───────────────────

    [Test]
    public async Task CA104_Preview_ChamaMaterializacaoAntesDeContar()
    {
        // Arrange
        var job = CriarJobEmRevisao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);

        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, default))
                 .ReturnsAsync(new List<MigracaoMapa>());

        var ordemDeExecucao = new List<string>();

        _registroRepo.Setup(r => r.DeletarPendentesPorJob(JobId, default))
                     .Callback(() => ordemDeExecucao.Add("deletar_pendentes"))
                     .Returns(Task.CompletedTask);
        _registroRepo.Setup(r => r.ListarPorJob(JobId, default))
                     .Callback(() => ordemDeExecucao.Add("listar_por_job"))
                     .ReturnsAsync(new List<MigracaoRegistro>());

        var sut = CriarSut();

        // Act
        await sut.Handle(JobId, AdminId, default);

        // Assert — DeletarPendentesPorJob (parte da materialização) deve ocorrer ANTES de ListarPorJob (contagem)
        var idxDeletar = ordemDeExecucao.IndexOf("deletar_pendentes");
        var idxListar  = ordemDeExecucao.IndexOf("listar_por_job");
        Assert.That(idxDeletar, Is.LessThan(idxListar),
            "CA104: materialização (deletar_pendentes) deve ocorrer antes da contagem (listar_por_job)");
    }

    // ─── CA104 — preview marca job como preview_pronto ───────────────────────────

    [Test]
    public async Task CA104_Preview_MarcaJobComoPreviewPronto()
    {
        var job = CriarJobEmRevisao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);
        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, default))
                 .ReturnsAsync(new List<MigracaoMapa>());
        _registroRepo.Setup(r => r.DeletarPendentesPorJob(JobId, default)).Returns(Task.CompletedTask);
        _registroRepo.Setup(r => r.ListarPorJob(JobId, default)).ReturnsAsync(new List<MigracaoRegistro>());

        var sut = CriarSut();
        await sut.Handle(JobId, AdminId, default);

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusPreviewPronto),
            "CA104: job deve avançar para preview_pronto após a materialização");
    }

    // ─── CA116 — preview com job não em revisão → BusinessException ─────────────

    [Test]
    public async Task StatusErrado_LancaBusinessException()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, Guid.NewGuid());
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        job.RegistrarArquivoRecebido("key");
        // Não avança para mapa_em_revisao

        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);

        var sut = CriarSut();

        Assert.ThrowsAsync<BusinessException>(() => sut.Handle(JobId, AdminId, default));
    }
}
