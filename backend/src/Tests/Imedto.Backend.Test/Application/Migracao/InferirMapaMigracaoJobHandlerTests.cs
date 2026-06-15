using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Imedto.Backend.Application.Migracao.Jobs;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Migracao;

[TestFixture]
public class InferirMapaMigracaoJobHandlerTests
{
    private Mock<IMigracaoJobRepository> _jobRepo;
    private Mock<IMigracaoArquivoStorageService> _storage;
    private Mock<IMapeadorDeMigracao> _mapeador;
    private Mock<IMigracaoMapaRepository> _mapaRepo;
    private Mock<IMigracaoTemplateRepository> _templateRepo;

    private static readonly Guid UsuarioId = Guid.NewGuid();
    private const long EstabelecimentoId = 42;
    private const long JobId = 99;

    [SetUp]
    public void SetUp()
    {
        _jobRepo      = new Mock<IMigracaoJobRepository>();
        _storage      = new Mock<IMigracaoArquivoStorageService>();
        _mapeador     = new Mock<IMapeadorDeMigracao>();
        _mapaRepo     = new Mock<IMigracaoMapaRepository>();
        _templateRepo = new Mock<IMigracaoTemplateRepository>();

        // Default: mapeador retorna proposta vazia.
        _mapeador.Setup(m => m.InferirMapaAsync(
                It.IsAny<EsquemaDeArquivo>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropostaDeMapa
            {
                DeParaColunas = new Dictionary<string, string> { ["nome"] = "nome" },
                Confianca = 0.9,
                Duvidas = [],
            });

        // Default: nenhum mapa existente (upsert cria novo).
        _mapaRepo.Setup(r => r.ObterPorJobEEntidadeOuNulo(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MigracaoMapa?)null);

        // Default: sem template para a origem.
        _templateRepo.Setup(r => r.ListarPorNome(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private InferirMapaMigracaoJobHandler CriarSut(params IMigracaoArquivoParser[] parsers)
    {
        return new InferirMapaMigracaoJobHandler(
            _jobRepo.Object,
            _storage.Object,
            _mapeador.Object,
            _mapaRepo.Object,
            _templateRepo.Object,
            parsers,
            NullLogger<InferirMapaMigracaoJobHandler>.Instance);
    }

    private static MigracaoJob CriarJobAguardandoMapa()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);

        // Usa reflection para setar Id (simula EF).
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);

        job.RegistrarArquivoRecebido("migracao/42/99/arquivo.zip");
        return job;
    }

    private static Stream CriarZipComArquivos(params (string nome, string conteudo)[] arquivos)
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (nome, conteudo) in arquivos)
            {
                var entry = zip.CreateEntry(nome);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(conteudo);
            }
        }
        ms.Position = 0;
        return ms;
    }

    // ─── CA5 — Amostra mascarada ──────────────────────────────────────────────

    /// <summary>CA5 — CPF na amostra deve ser mascarado antes de chegar ao mapeador.</summary>
    [Test]
    public async Task ExecutarAsync_CpfNaAmostra_MascaradoAntesDaInferencia()
    {
        var csvComCpf = "nome,cpf\nJoao,123.456.789-00\n";
        var zip = CriarZipComArquivos(("pacientes.csv", csvComCpf));

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

        EsquemaDeArquivo? esquemaCapturado = null;
        _mapeador.Setup(m => m.InferirMapaAsync(
                It.IsAny<EsquemaDeArquivo>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<EsquemaDeArquivo, string, CancellationToken>((e, _, _) => esquemaCapturado = e)
            .ReturnsAsync(new PropostaDeMapa
            {
                DeParaColunas = new Dictionary<string, string>(),
                Confianca = 0.5,
                Duvidas = [],
            });

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // CPF original é "123.456.789-00" — deve ter sido mascarado.
        Assert.That(esquemaCapturado, Is.Not.Null);
        var cpfNaAmostra = esquemaCapturado!.AmostraMascarada[0]["cpf"];
        Assert.That(cpfNaAmostra, Does.Not.Contain("123.456.789-00"));
        Assert.That(cpfNaAmostra, Does.Contain("REDACTED").Or.Contain("redacted"));
    }

    // ─── CA23 — 1 chamada por arquivo ────────────────────────────────────────

    /// <summary>CA23 — ZIP com 2 arquivos deve resultar em 2 chamadas ao mapeador.</summary>
    [Test]
    public async Task ExecutarAsync_ZipCom2Arquivos_2ChamadasAoMapeador()
    {
        var zip = CriarZipComArquivos(
            ("pacientes.csv", "nome,email\nJoao,j@a.com\n"),
            ("agendamentos.csv", "data,paciente\n2024-01-01,Joao\n")
        );

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // Deve ter chamado InferirMapaAsync exatamente 2 vezes (1 por arquivo).
        _mapeador.Verify(m => m.InferirMapaAsync(
            It.IsAny<EsquemaDeArquivo>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    /// <summary>Quando não há jobs aguardando, não deve chamar nenhuma dependência pesada.</summary>
    [Test]
    public async Task ExecutarAsync_SemJobs_NaoChamaDependencias()
    {
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync((MigracaoJob?)null);

        var sut = CriarSut();
        await sut.ExecutarAsync(CancellationToken.None);

        _storage.Verify(s => s.DownloadArquivoAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mapeador.Verify(m => m.InferirMapaAsync(
            It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── CA18 — Template pré-carregado ──────────────────────────────────────

    /// <summary>
    /// CA18 — quando há template para a origem do job, o mapeador de IA NÃO é chamado
    /// para a entidade coberta pelo template. O mapa é persistido com o JSON do template.
    /// </summary>
    [Test]
    public async Task ExecutarAsync_TemTemplateDaOrigem_NaoChamaIAParaEntidadeComTemplate()
    {
        const string origem = "iClinic";
        const string mapaJsonTemplate = """{"de_para":{"nome_paciente":"nome","cpf_paciente":"cpf"},"confianca":1.0,"duvidas":[]}""";

        var zip = CriarZipComArquivos(("pacientes.csv", "nome_paciente,cpf_paciente\nJoao,***\n"));

        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId, origem);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        job.RegistrarArquivoRecebido("migracao/42/99/arquivo.zip");

        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

        // Template existe para "paciente" nesta origem.
        var template = MigracaoTemplate.Criar("iClinic", "paciente", mapaJsonTemplate, UsuarioId);
        _templateRepo.Setup(r => r.ListarPorNome(origem, It.IsAny<CancellationToken>()))
            .ReturnsAsync([template]);

        string? mapaJsonSalvo = null;
        _mapaRepo.Setup(r => r.Salvar(It.IsAny<MigracaoMapa>(), It.IsAny<CancellationToken>()))
            .Callback<MigracaoMapa, CancellationToken>((m, _) => mapaJsonSalvo = m.MapaJson)
            .Returns(Task.CompletedTask);

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // IA NÃO deve ser chamada — template cobre a entidade (CA18).
        _mapeador.Verify(m => m.InferirMapaAsync(
            It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never, "IA não deve ser chamada quando há template para a entidade.");

        // Mapa persistido deve usar o JSON do template.
        Assert.That(mapaJsonSalvo, Is.Not.Null);
        Assert.That(mapaJsonSalvo, Does.Contain("nome_paciente"));
    }
}
