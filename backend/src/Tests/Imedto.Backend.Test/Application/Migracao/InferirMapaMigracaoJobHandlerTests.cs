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
    private Mock<IMigracaoJobEventoRepository> _eventoRepo;

    private static readonly Guid UsuarioId = Guid.NewGuid();
    private const long EstabelecimentoId = 42;
    private const long JobId = 99;

    // Resposta padrão InferirBlocoAsync (addendum 4 — novo método do handler).
    private static readonly PropostaDeBlocoMapeado PropostaBloco = new()
    {
        EntidadeClassificada = EntidadesCanônicas.Paciente,
        ConfiancaClassificacao = 0.9,
        DeParaColunas = new Dictionary<string, string> { ["nome"] = "nome" },
        Confianca = 0.9,
        Duvidas = [],
    };

    [SetUp]
    public void SetUp()
    {
        _jobRepo      = new Mock<IMigracaoJobRepository>();
        _storage      = new Mock<IMigracaoArquivoStorageService>();
        _eventoRepo   = new Mock<IMigracaoJobEventoRepository>();
        _eventoRepo.Setup(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);
        _mapeador     = new Mock<IMapeadorDeMigracao>();
        _mapaRepo     = new Mock<IMigracaoMapaRepository>();
        _templateRepo = new Mock<IMigracaoTemplateRepository>();

        // Default: mapeador retorna proposta padrão (addendum 4 — InferirBlocoAsync).
        _mapeador.Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PropostaBloco);

        // Default: nenhum mapa existente (upsert cria novo).
        _mapaRepo.Setup(r => r.ObterPorJobEntidadeBlocoOuNulo(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
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
            _eventoRepo.Object,
            parsers,
            NullLogger<InferirMapaMigracaoJobHandler>.Instance);
    }

    private static MigracaoJob CriarJobAguardandoMapa()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        job.RegistrarArquivoRecebido("migracao/42/99/arquivo.zip");
        job.AprovarAnalise(Guid.NewGuid());
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

    /// <summary>CA5/CA75 — CPF na amostra deve ser mascarado antes de chegar ao mapeador.</summary>
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
        _mapeador.Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<EsquemaDeArquivo, string, CancellationToken>((e, _, _) => esquemaCapturado = e)
            .ReturnsAsync(PropostaBloco);

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        Assert.That(esquemaCapturado, Is.Not.Null);
        var cpfNaAmostra = esquemaCapturado!.AmostraMascarada[0]["cpf"];
        Assert.That(cpfNaAmostra, Does.Not.Contain("123.456.789-00"));
        Assert.That(cpfNaAmostra, Does.Contain("REDACTED").Or.Contain("redacted"));
    }

    // ─── CA85 / CA74 — Regressão: CSV 2 arquivos = 2 blocos = 2 chamadas ───────

    /// <summary>
    /// CA85/CA74 — ZIP com 2 arquivos CSV deve resultar em 2 chamadas ao mapeador (1 por bloco).
    /// Regressão: o caminho tabular existente não pode regredir.
    /// </summary>
    [Test]
    public async Task ExecutarAsync_ZipCom2ArquivosCsv_2ChamadasAoMapeador()
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

        _mapeador.Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PropostaBloco);

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // 2 arquivos CSV = 2 blocos = 2 chamadas (CA74 — 1 por bloco).
        _mapeador.Verify(m => m.InferirBlocoAsync(
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
        _mapeador.Verify(m => m.InferirBlocoAsync(
            It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── CA18 — Template pré-carregado ──────────────────────────────────────

    /// <summary>
    /// CA18 — quando há template para a origem do job, o mapeador de IA NÃO é chamado
    /// para a entidade coberta pelo template.
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
        job.AprovarAnalise(Guid.NewGuid());

        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

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
        _mapeador.Verify(m => m.InferirBlocoAsync(
            It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never, "IA não deve ser chamada quando há template para a entidade.");

        Assert.That(mapaJsonSalvo, Is.Not.Null);
        Assert.That(mapaJsonSalvo, Does.Contain("nome_paciente"));
    }

    // ─── CA70 — Dump aninhado vira N blocos ──────────────────────────────────

    /// <summary>
    /// CA70 — Dump JSON com objeto raiz contendo 3 arrays + 1 objeto config deve gerar
    /// 3 blocos migráveis (pacientes, agendamentos, reparticoes) + 1 config (estabelecimento).
    /// O config é persistido como sem_equivalente/ignorado; os 3 arrays disparam inferência.
    /// </summary>
    [Test]
    public async Task ExecutarAsync_DumpAninhado_GeraUmBlocoPorArray()
    {
        const string jsonDump = """
            {
                "estabelecimento": { "nome": "Clinica X", "cnpj": "00.000.000/0001-00" },
                "reparticoes": [{ "id": 1, "nome": "Sala A" }, { "id": 2, "nome": "Sala B" }],
                "pacientes": [{ "nome": "Joao", "cpf": "000.000.000-00" }],
                "agendamentos": [{ "data": "2024-01-01", "paciente": "Joao" }]
            }
            """;

        var zip = CriarZipComArquivos(("dump_sistema_x.json", jsonDump));

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

        _mapeador.Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropostaDeBlocoMapeado
            {
                EntidadeClassificada = EntidadesCanônicas.SemEquivalente,
                ConfiancaClassificacao = 0.5,
                DeParaColunas = new Dictionary<string, string>(),
                Confianca = 0.5,
                Duvidas = [],
            });

        var parser = new Imedto.Backend.Infrastructure.Migracao.JsonMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // 3 arrays de objetos (reparticoes, pacientes, agendamentos) = 3 chamadas à IA.
        // estabelecimento{} é config — persistido sem chamar IA.
        _mapeador.Verify(m => m.InferirBlocoAsync(
            It.IsAny<EsquemaDeArquivo>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3),
            "Deve haver 1 chamada por array de objetos (não pelo objeto config).");

        // 4 mapas no total: 3 inferidos + 1 config.
        _mapaRepo.Verify(r => r.Salvar(
            It.IsAny<MigracaoMapa>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    // ─── CA71 — JSON-array na raiz não regride ────────────────────────────────

    /// <summary>
    /// CA71 — JSON-array na raiz deve ser tratado como 1 bloco (caminho existente não regride).
    /// </summary>
    [Test]
    public async Task ExecutarAsync_JsonArrayNaRaiz_1BlocoNaoRegride()
    {
        const string jsonArray = """[{"nome":"Joao","email":"j@a.com"},{"nome":"Maria","email":"m@b.com"}]""";
        var zip = CriarZipComArquivos(("pacientes.json", jsonArray));

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

        _mapeador.Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PropostaBloco);

        var parser = new Imedto.Backend.Infrastructure.Migracao.JsonMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // JSON-array = 1 bloco = 1 chamada (CA71/CA74).
        _mapeador.Verify(m => m.InferirBlocoAsync(
            It.IsAny<EsquemaDeArquivo>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── CA72 — Sub-objetos não mapeados, não inventados ─────────────────────

    /// <summary>
    /// CA72 — Registro com campo sub-objeto (campos_especificos) e array (ids) não deve
    /// incluir esses campos nos Cabecalhos enviados à IA (R-S2/D-S4).
    /// </summary>
    [Test]
    public async Task ExecutarAsync_RegistroComSubObjeto_NaoIncluiSubObjetoNoCabecalho()
    {
        const string jsonDump = """
            {
                "pacientes": [
                    {
                        "nome": "Joao",
                        "campos_especificos": { "alergias": "nenhuma" },
                        "ids_relacionados": [1, 2, 3]
                    }
                ]
            }
            """;

        var zip = CriarZipComArquivos(("dump.json", jsonDump));

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

        EsquemaDeArquivo? esquemaCapturado = null;
        _mapeador.Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<EsquemaDeArquivo, string, CancellationToken>((e, _, _) => esquemaCapturado = e)
            .ReturnsAsync(PropostaBloco);

        var parser = new Imedto.Backend.Infrastructure.Migracao.JsonMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // Cabeçalhos enviados à IA: só "nome" (plano). Sub-objetos e arrays excluídos.
        Assert.That(esquemaCapturado, Is.Not.Null);
        Assert.That(esquemaCapturado!.Cabecalhos, Does.Not.Contain("campos_especificos"),
            "Sub-objeto não deve aparecer nos cabeçalhos enviados à IA (D-S4).");
        Assert.That(esquemaCapturado.Cabecalhos, Does.Not.Contain("ids_relacionados"),
            "Array interno não deve aparecer nos cabeçalhos enviados à IA (D-S4).");
        Assert.That(esquemaCapturado.Cabecalhos, Does.Contain("nome"),
            "Campo plano deve aparecer nos cabeçalhos.");
    }

    // ─── CA73 — Entidade classificada pela IA, não pelo nome do arquivo ──────

    /// <summary>
    /// CA73 — O nome do arquivo (dump_sistema_hospitalar_backup_2026.json) não deve ser
    /// usado como entidade. A IA classifica pelo schema do bloco.
    /// </summary>
    [Test]
    public async Task ExecutarAsync_NomeArquivoInutil_EntidadeClassificadaPelaIA()
    {
        const string jsonDump = """{"pacientes":[{"nome":"Joao","cpf":"000.000.000-00","data_nascimento":"1990-01-01"}]}""";
        var zip = CriarZipComArquivos(("dump_sistema_hospitalar_backup_2026.json", jsonDump));

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

        string? entidadeClassificadaPersistida = null;
        _mapaRepo.Setup(r => r.Salvar(It.IsAny<MigracaoMapa>(), It.IsAny<CancellationToken>()))
            .Callback<MigracaoMapa, CancellationToken>((m, _) => entidadeClassificadaPersistida = m.Entidade)
            .Returns(Task.CompletedTask);

        // IA classifica como "paciente" (pelo schema do bloco, não pelo nome do arquivo).
        _mapeador.Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropostaDeBlocoMapeado
            {
                EntidadeClassificada = EntidadesCanônicas.Paciente,
                ConfiancaClassificacao = 0.95,
                DeParaColunas = new Dictionary<string, string> { ["nome"] = "nome" },
                Confianca = 0.9,
                Duvidas = [],
            });

        var parser = new Imedto.Backend.Infrastructure.Migracao.JsonMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // Entidade persistida = "paciente" (classificada pela IA), não "dump_sistema_hospitalar_backup_2026".
        Assert.That(entidadeClassificadaPersistida, Is.EqualTo(EntidadesCanônicas.Paciente),
            "Entidade deve ser classificada pela IA, não extraída do nome do arquivo (CA73).");
    }

    // ─── CA74 — Teto de 1 chamada por bloco ──────────────────────────────────

    /// <summary>
    /// CA74 — Dump com 5 blocos = no máximo 5 chamadas ao provider (1 por bloco).
    /// </summary>
    [Test]
    public async Task ExecutarAsync_DumpCom5Blocos_Exatamente5ChamadasAoProvider()
    {
        const string jsonDump = """
            {
                "a": [{"id":1}],
                "b": [{"id":2}],
                "c": [{"id":3}],
                "d": [{"id":4}],
                "e": [{"id":5}]
            }
            """;

        var zip = CriarZipComArquivos(("dump5.json", jsonDump));

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

        _mapeador.Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropostaDeBlocoMapeado
            {
                EntidadeClassificada = EntidadesCanônicas.SemEquivalente,
                ConfiancaClassificacao = 0.3,
                DeParaColunas = new Dictionary<string, string>(),
                Confianca = 0.3,
                Duvidas = [],
            });

        var parser = new Imedto.Backend.Infrastructure.Migracao.JsonMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // 5 blocos = exatamente 5 chamadas (teto do CA74).
        _mapeador.Verify(m => m.InferirBlocoAsync(
            It.IsAny<EsquemaDeArquivo>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(5),
            "Não deve ultrapassar 1 chamada por bloco (CA74/D-N2).");
    }
}
