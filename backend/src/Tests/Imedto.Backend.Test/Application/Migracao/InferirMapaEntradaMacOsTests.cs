using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Imedto.Backend.Application.Migracao.Jobs;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Migracao;

/// <summary>
/// Testes de regressão para entradas de lixo geradas pelo macOS/Finder em ZIPs.
///
/// ZIPs criados pelo macOS contêm entradas AppleDouble (__MACOSX/._&lt;nome&gt;)
/// com conteúdo binário. Essas entradas terminam em extensão legítima (.json, .csv),
/// fazem o parser ser selecionado e lançam JsonException/CsvException ao parsear,
/// derrubando o job inteiro sem o filtro adequado (bug do job #12).
///
/// Cobre:
/// - Camada 1: filtro de entrada (__MACOSX/ e ._nome) antes do parser.
/// - Camada 2: try-catch no ParsearAsync — arquivo não-parseável não derruba o job.
/// - Regressão: comportamento normal (dump aninhado, reprocessar parcial) não é afetado.
/// </summary>
[TestFixture]
public class InferirMapaEntradaMacOsTests
{
    private Mock<IMigracaoJobRepository>       _jobRepo;
    private Mock<IMigracaoArquivoStorageService> _storage;
    private Mock<IMapeadorDeMigracao>          _mapeador;
    private Mock<IMigracaoMapaRepository>      _mapaRepo;
    private Mock<IMigracaoTemplateRepository>  _templateRepo;
    private Mock<IMigracaoJobEventoRepository> _eventoRepo;

    private static readonly Guid UsuarioId = Guid.NewGuid();
    private const long EstabelecimentoId = 42L;
    private const long JobId = 12L;

    private static readonly PropostaDeBlocoMapeado PropostaSucesso = new()
    {
        EntidadeClassificada   = EntidadesCanônicas.Paciente,
        ConfiancaClassificacao = 0.9,
        DeParaColunas          = new Dictionary<string, string> { ["nome"] = "nome" },
        Confianca              = 0.9,
        Duvidas                = [],
    };

    [SetUp]
    public void SetUp()
    {
        _jobRepo      = new Mock<IMigracaoJobRepository>();
        _storage      = new Mock<IMigracaoArquivoStorageService>();
        _mapeador     = new Mock<IMapeadorDeMigracao>();
        _mapaRepo     = new Mock<IMigracaoMapaRepository>();
        _templateRepo = new Mock<IMigracaoTemplateRepository>();
        _eventoRepo   = new Mock<IMigracaoJobEventoRepository>();

        _eventoRepo
            .Setup(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _templateRepo
            .Setup(r => r.ListarPorNome(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _mapaRepo
            .Setup(r => r.ObterPorJobEntidadeBlocoOuNulo(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MigracaoMapa?)null);
        _mapaRepo
            .Setup(r => r.ObterPorJobBlocoOuNulo(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MigracaoMapa?)null);
        _mapaRepo
            .Setup(r => r.Salvar(It.IsAny<MigracaoMapa>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mapeador
            .Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PropostaSucesso);
    }

    private InferirMapaMigracaoJobHandler CriarSut(params IMigracaoArquivoParser[] parsers)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Ia:PausaEntreBlocosMs"] = "0" })
            .Build();

        return new InferirMapaMigracaoJobHandler(
            _jobRepo.Object,
            _storage.Object,
            _mapeador.Object,
            _mapaRepo.Object,
            _templateRepo.Object,
            _eventoRepo.Object,
            parsers,
            NullLogger<InferirMapaMigracaoJobHandler>.Instance,
            config);
    }

    private MigracaoJob CriarJobAguardandoMapa()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        job.RegistrarArquivoRecebido("migracao/42/12/arquivo.zip");
        job.AprovarAnalise(Guid.NewGuid());
        return job;
    }

    /// <summary>
    /// Monta ZIP sintético equivalente ao job #12 do macOS:
    /// - dados.json (JSON válido com array de objetos).
    /// - __MACOSX/._dados.json (binário AppleDouble).
    /// Adiciona opcionalmente uma entrada de diretório (__MACOSX/).
    /// </summary>
    private static Stream CriarZipMacOsComJsonValido(string nomeArquivo, string conteudoJson)
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            // Arquivo real
            var entryReal = zip.CreateEntry(nomeArquivo);
            using (var writer = new StreamWriter(entryReal.Open()))
                writer.Write(conteudoJson);

            // Diretório __MACOSX/ (como o Finder cria)
            zip.CreateEntry("__MACOSX/");

            // Metadado AppleDouble: binário (bytes típicos de AppleDouble = 0x00 0x05 0x16 0x07 ...)
            var entryApple = zip.CreateEntry($"__MACOSX/._{nomeArquivo}");
            using var appleStream = entryApple.Open();
            // Simula cabeçalho AppleDouble binário — não é JSON válido.
            var bytesAppleDouble = new byte[] { 0x00, 0x05, 0x16, 0x07, 0x00, 0x02, 0x00, 0x00 };
            appleStream.Write(bytesAppleDouble);
        }
        ms.Position = 0;
        return ms;
    }

    // ─── Camada 1: filtro __MACOSX ───────────────────────────────────────────

    /// <summary>
    /// Bug do job #12: ZIP com __MACOSX/._dados.json (binário) + dados.json (JSON válido).
    /// O handler deve ignorar a entrada AppleDouble e processar só o arquivo real.
    /// Job NÃO deve ir para 'falhou'.
    /// </summary>
    [Test]
    public async Task ExecutarAsync_ZipMacOs_EntradaAppleDouble_Ignorada_JobNaoFalha()
    {
        const string jsonValido = """[{"nome":"Joao","email":"j@clinica.com"},{"nome":"Maria","email":"m@clinica.com"}]""";
        var zip = CriarZipMacOsComJsonValido("dados.json", jsonValido);

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

        MigracaoJob? jobSalvo = null;
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Callback<MigracaoJob, CancellationToken>((j, _) => jobSalvo = j)
            .Returns(Task.CompletedTask);

        var parser = new Imedto.Backend.Infrastructure.Migracao.JsonMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // Job deve ir para mapa_em_revisao, NÃO para falhou.
        Assert.That(jobSalvo?.Status, Is.EqualTo(MigracaoJob.StatusMapaEmRevisao),
            "Job com ZIP macOS não deve ir para 'falhou' — entrada AppleDouble deve ser ignorada.");
    }

    /// <summary>
    /// Camada 1 (a): entrada com prefixo __MACOSX/ é ignorada, mesmo com extensão .json.
    /// Verifica que o mapeador é chamado exatamente 1 vez (só pelo arquivo real).
    /// </summary>
    [Test]
    public async Task ExecutarAsync_ZipMacOs_MapeadorChamadoSo1Vez_ParaArquivoReal()
    {
        const string jsonValido = """[{"nome":"Joao","cpf":"***"}]""";
        var zip = CriarZipMacOsComJsonValido("pacientes.json", jsonValido);

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var parser = new Imedto.Backend.Infrastructure.Migracao.JsonMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // Apenas 1 chamada — a entrada __MACOSX/._pacientes.json foi ignorada.
        _mapeador.Verify(m => m.InferirBlocoAsync(
            It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Mapeador deve ser chamado exatamente 1 vez — entrada AppleDouble ignorada (camada 1).");
    }

    /// <summary>
    /// Camada 1 (b): entrada de diretório (FullName termina em '/') não causa erro de parse.
    /// </summary>
    [Test]
    public async Task ExecutarAsync_ZipComEntradaDiretorio_DiretorioIgnorado()
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            // Diretório explícito
            zip.CreateEntry("subdir/");
            // Arquivo real dentro do diretório
            var entry = zip.CreateEntry("subdir/pacientes.csv");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("nome,email\nJoao,j@a.com\n");
        }
        ms.Position = 0;

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ms);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        // Não deve lançar exceção ao processar entrada de diretório.
        Assert.DoesNotThrowAsync(() => sut.ExecutarAsync(CancellationToken.None),
            "Entrada de diretório no ZIP não deve causar exceção (camada 1).");
    }

    // ─── Camada 2: try-catch no ParsearAsync ────────────────────────────────

    /// <summary>
    /// Camada 2 (defesa em profundidade): arquivo com extensão .json mas conteúdo binário
    /// (não filtrado pela camada 1 por algum motivo) não derruba o job.
    /// Os demais arquivos válidos do ZIP continuam sendo processados.
    /// </summary>
    [Test]
    public async Task ExecutarAsync_ArquivoBinarioComExtensaoJson_NaoDerrubaJob()
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            // Arquivo corrompido: extensão .json mas conteúdo binário inválido.
            var entryBinario = zip.CreateEntry("corrompido.json");
            using (var s = entryBinario.Open())
                s.Write(new byte[] { 0xFF, 0xFE, 0x00, 0xAB, 0xCD }); // não é JSON

            // Arquivo real válido (CSV).
            var entryCsv = zip.CreateEntry("pacientes.csv");
            using var writer = new StreamWriter(entryCsv.Open());
            writer.Write("nome,email\nJoao,j@a.com\n");
        }
        ms.Position = 0;

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ms);

        MigracaoJob? jobSalvo = null;
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Callback<MigracaoJob, CancellationToken>((j, _) => jobSalvo = j)
            .Returns(Task.CompletedTask);

        // Parsers: JSON + CSV. O JSON vai falhar para o binário; o CSV vai processar pacientes.csv.
        var jsonParser = new Imedto.Backend.Infrastructure.Migracao.JsonMigracaoParser();
        var csvParser  = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(jsonParser, csvParser);

        await sut.ExecutarAsync(CancellationToken.None);

        // Job deve ir para mapa_em_revisao — o CSV foi processado mesmo com o JSON corrompido.
        Assert.That(jobSalvo?.Status, Is.EqualTo(MigracaoJob.StatusMapaEmRevisao),
            "Arquivo binário não-parseável não deve derrubar job — CSV válido deve ser processado (camada 2).");

        // Mapeador chamado exatamente 1 vez (pelo CSV — o JSON falhou no parse).
        _mapeador.Verify(m => m.InferirBlocoAsync(
            It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Mapeador deve ser chamado 1 vez — só o arquivo CSV válido chega ao mapeador (camada 2).");
    }

    // ─── Regressão: comportamento normal não afetado ─────────────────────────

    /// <summary>
    /// Regressão: dump JSON aninhado real (sem lixo macOS) ainda gera N blocos.
    /// O filtro de macOS não deve afetar o caminho normal.
    /// </summary>
    [Test]
    public async Task ExecutarAsync_DumpAninhadoSemLixoMacOs_GeraBloCosNormalmente()
    {
        const string jsonDump = """
            {
                "pacientes": [{"nome": "Joao", "cpf": "***"}],
                "agendamentos": [{"data": "2024-01-01", "paciente": "Joao"}]
            }
            """;

        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry("sistema_hospitalar_backup_2025-12-09.json");
            using var writer = new StreamWriter(entry.Open());
            writer.Write(jsonDump);
        }
        ms.Position = 0;

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ms);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var parser = new Imedto.Backend.Infrastructure.Migracao.JsonMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // 2 arrays no dump = 2 chamadas ao mapeador. Regressão não pode regredir.
        _mapeador.Verify(m => m.InferirBlocoAsync(
            It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2),
            "Dump aninhado sem lixo macOS deve gerar 1 chamada por bloco (regressão).");
    }

    /// <summary>
    /// Regressão: ZIP do job #12 real tem os 17 blocos já mapeados (reprocessar parcial).
    /// Com o fix, o __MACOSX/._* é ignorado e os blocos OK são pulados (CA97).
    /// Job vai para mapa_em_revisao (≥1 sucesso via reprocessar parcial conta como OK).
    /// </summary>
    [Test]
    public async Task ExecutarAsync_Job12MacOs_BlocosJaMapeados_VaiParaMapaEmRevisao()
    {
        const string jsonValido = """[{"nome":"Joao"},{"nome":"Maria"}]""";
        var zip = CriarZipMacOsComJsonValido("sistema_hospitalar_backup_2025-12-09.json", jsonValido);

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

        // Simula bloco já mapeado com sucesso (como no job #12 real).
        var mapaOk = MigracaoMapa.Criar(
            JobId, EstabelecimentoId, EntidadesCanônicas.Paciente,
            """{"de_para":{"nome":"nome"},"confianca":0.9,"duvidas":[],"entidade_classificada":"paciente","confianca_classificacao":0.9,"ignorado":false,"encoding_suspeito":false}""",
            "sistema_hospitalar_backup_2025-12-09");
        _mapaRepo
            .Setup(r => r.ObterPorJobBlocoOuNulo(
                JobId, "sistema_hospitalar_backup_2025-12-09", EstabelecimentoId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapaOk);

        MigracaoJob? jobSalvo = null;
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Callback<MigracaoJob, CancellationToken>((j, _) => jobSalvo = j)
            .Returns(Task.CompletedTask);

        var parser = new Imedto.Backend.Infrastructure.Migracao.JsonMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // Bloco já mapeado → pula IA. Job vai para mapa_em_revisao.
        Assert.That(jobSalvo?.Status, Is.EqualTo(MigracaoJob.StatusMapaEmRevisao),
            "Job #12 reprocessado deve ir para mapa_em_revisao (blocos já mapeados + macOS ignorado).");

        // IA não chamada — bloco já OK (CA97).
        _mapeador.Verify(m => m.InferirBlocoAsync(
            It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "IA não deve ser chamada — bloco do job #12 já está mapeado (CA97).");
    }
}
