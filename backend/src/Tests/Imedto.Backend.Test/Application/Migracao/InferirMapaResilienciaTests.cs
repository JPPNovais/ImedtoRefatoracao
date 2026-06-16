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
/// Testes de resiliência da inferência de mapa — addendum 5 (CA90-CA98, CA99).
/// Cobre: truncamento pós-máscara, degradação por bloco, reprocessar parcial, LGPD.
/// </summary>
[TestFixture]
public class InferirMapaResilienciaTests
{
    private Mock<IMigracaoJobRepository>      _jobRepo;
    private Mock<IMigracaoArquivoStorageService> _storage;
    private Mock<IMapeadorDeMigracao>         _mapeador;
    private Mock<IMigracaoMapaRepository>     _mapaRepo;
    private Mock<IMigracaoTemplateRepository> _templateRepo;
    private Mock<IMigracaoJobEventoRepository> _eventoRepo;

    private static readonly Guid UsuarioId = Guid.NewGuid();
    private const long EstabelecimentoId = 42L;
    private const long JobId = 77L;

    // Resposta bem-sucedida padrão.
    private static readonly PropostaDeBlocoMapeado PropostaSucesso = new()
    {
        EntidadeClassificada    = EntidadesCanônicas.Paciente,
        ConfiancaClassificacao  = 0.9,
        DeParaColunas           = new Dictionary<string, string> { ["nome"] = "nome" },
        Confianca               = 0.9,
        Duvidas                 = [],
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
    }

    private InferirMapaMigracaoJobHandler CriarSut(
        params IMigracaoArquivoParser[] parsers)
    {
        // Pausa 0ms para não travar os testes.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ia:PausaEntreBlocosMs"] = "0"
            })
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
        job.RegistrarArquivoRecebido("migracao/42/77/arquivo.zip");
        job.AprovarAnalise(Guid.NewGuid());
        return job;
    }

    private static Stream CriarZipComCsv(params (string nome, string conteudo)[] arquivos)
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

    // ─── CA91 — truncamento de valor na amostra (após máscara de PII) ────────

    [Test]
    public async Task ExecutarAsync_ValorGrande_TruncadoA500CharsNaAmostra()
    {
        // Campo conteudo_html com 5000 chars.
        var valorGrande = new string('X', 5000);
        var csv = $"nome,conteudo_html\nJoao,{valorGrande}\n";
        var zip = CriarZipComCsv(("dados.csv", csv));

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

        EsquemaDeArquivo? esquemaCapturado = null;
        _mapeador
            .Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<EsquemaDeArquivo, string, CancellationToken>((e, _, _) => esquemaCapturado = e)
            .ReturnsAsync(PropostaSucesso);

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        Assert.That(esquemaCapturado, Is.Not.Null);
        var valorNaAmostra = esquemaCapturado!.AmostraMascarada[0]["conteudo_html"];
        Assert.That(valorNaAmostra.Length, Is.LessThanOrEqualTo(500 + "[truncado]".Length + 5),
            "Valor deve ter no máximo 500 chars + marcador de truncamento (CA91).");
        Assert.That(valorNaAmostra, Does.Contain("[truncado]") | Does.EndWith("[truncado]"),
            "Marcador de truncamento deve estar presente.");
    }

    [Test]
    public async Task ExecutarAsync_ValorPequeno_NaoTruncado()
    {
        var valorPequeno = "joao silva";
        var csv = $"nome,email\n{valorPequeno},j@a.com\n";
        var zip = CriarZipComCsv(("dados.csv", csv));

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

        EsquemaDeArquivo? esquemaCapturado = null;
        _mapeador
            .Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<EsquemaDeArquivo, string, CancellationToken>((e, _, _) => esquemaCapturado = e)
            .ReturnsAsync(PropostaSucesso);

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        Assert.That(esquemaCapturado, Is.Not.Null);
        var nomeNaAmostra = esquemaCapturado!.AmostraMascarada[0]["nome"];
        Assert.That(nomeNaAmostra, Does.Not.Contain("[truncado]"),
            "Valor curto não deve ter marcador de truncamento.");
    }

    // ─── CA92 — falha de bloco após retry vira mapa de erro, não derruba job ─

    [Test]
    public async Task ExecutarAsync_BlocoFalha_NaoDerruba_Job_E_ContinuaOutrosBlocos()
    {
        // 2 arquivos CSV: o 1º vai falhar na IA; o 2º deve ser processado normalmente.
        var zip = CriarZipComCsv(
            ("pacientes.csv", "nome,cpf\nJoao,***\n"),
            ("agendamentos.csv", "data,paciente\n2024-01-01,Joao\n")
        );

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

        var chamadasMapeador = 0;
        _mapeador
            .Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                chamadasMapeador++;
                if (chamadasMapeador == 1)
                    throw new InvalidOperationException("limite_taxa_ia: falha simulada");
                return PropostaSucesso;
            });

        MigracaoJob? jobSalvo = null;
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Callback<MigracaoJob, CancellationToken>((j, _) => jobSalvo = j)
            .Returns(Task.CompletedTask);

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // Job deve estar em mapa_em_revisao (não falhou) — CA94.
        Assert.That(jobSalvo?.Status, Is.EqualTo(MigracaoJob.StatusMapaEmRevisao),
            "Job deve ir para mapa_em_revisao quando ao menos 1 bloco mapeou (CA94).");
        // 2 chamadas ao mapeador: 1 falhou, 1 sucesso.
        Assert.That(chamadasMapeador, Is.EqualTo(2),
            "A inferência deve continuar com o 2º bloco após falha do 1º (CA92).");
    }

    // ─── CA93 — blocos bem-sucedidos são preservados quando outro falha ───────

    [Test]
    public async Task ExecutarAsync_1BlocoFalha_OutrosBlocosSalvos()
    {
        var zip = CriarZipComCsv(
            ("pacientes.csv", "nome\nJoao\n"),   // sucesso
            ("agendamentos.csv", "data\n2024-01-01\n") // falha
        );

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);

        var chamada = 0;
        _mapeador
            .Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                chamada++;
                return chamada == 1 ? PropostaSucesso
                    : throw new InvalidOperationException("limite_taxa_ia");
            });

        var mapasSalvos = new List<MigracaoMapa>();
        _mapaRepo.Setup(r => r.Salvar(It.IsAny<MigracaoMapa>(), It.IsAny<CancellationToken>()))
            .Callback<MigracaoMapa, CancellationToken>((m, _) => mapasSalvos.Add(m))
            .Returns(Task.CompletedTask);

        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // 2 mapas salvos: 1 sucesso + 1 erro (CA92/CA93).
        Assert.That(mapasSalvos.Count, Is.EqualTo(2),
            "Deve haver 1 mapa de sucesso + 1 mapa de erro (CA93).");

        // O mapa de erro tem bloco_com_erro = true.
        var mapaErro = mapasSalvos.FirstOrDefault(m =>
        {
            using var doc = JsonDocument.Parse(m.MapaJson);
            return doc.RootElement.TryGetProperty("bloco_com_erro", out var v)
                   && v.ValueKind == JsonValueKind.True;
        });
        Assert.That(mapaErro, Is.Not.Null, "Deve existir mapa com bloco_com_erro=true (CA92).");
    }

    // ─── CA95 — zero sucesso → falhou ────────────────────────────────────────

    [Test]
    public async Task ExecutarAsync_TodosBlocosFalham_JobVaiParaFalhou()
    {
        var zip = CriarZipComCsv(
            ("a.csv", "nome\nJoao\n"),
            ("b.csv", "data\n2024-01-01\n")
        );

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);
        _mapeador
            .Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("limite_taxa_ia: todos falharam"));

        MigracaoJob? jobSalvo = null;
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Callback<MigracaoJob, CancellationToken>((j, _) => jobSalvo = j)
            .Returns(Task.CompletedTask);

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // Todos falharam → falhou (CA95/R-R6).
        Assert.That(jobSalvo?.Status, Is.EqualTo(MigracaoJob.StatusFalhou),
            "Zero sucesso → job vai para 'falhou' (CA95).");
    }

    // ─── CA99 — LGPD: motivo de erro é categoria genérica sem PII ────────────

    [Test]
    public async Task ExecutarAsync_BlocoErro_MotivoGenerico_SemPii()
    {
        var csv = "cpf,nome\n123.456.789-00,Joao Silva\n";
        var zip = CriarZipComCsv(("pacientes.csv", csv));

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);
        _mapeador
            .Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("limite_taxa_ia: bloco falhou"));
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        MigracaoMapa? mapaErroSalvo = null;
        _mapaRepo.Setup(r => r.Salvar(It.IsAny<MigracaoMapa>(), It.IsAny<CancellationToken>()))
            .Callback<MigracaoMapa, CancellationToken>((m, _) => mapaErroSalvo = m)
            .Returns(Task.CompletedTask);

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        Assert.That(mapaErroSalvo, Is.Not.Null, "Mapa de erro deve ser persistido.");

        using var doc = JsonDocument.Parse(mapaErroSalvo!.MapaJson);
        var motivoErro = doc.RootElement.GetProperty("motivo_erro").GetString();

        // CA99: motivo deve ser categoria genérica sem PII.
        Assert.That(motivoErro, Is.Not.Null.And.Not.Empty);
        Assert.That(motivoErro, Does.Not.Contain("123.456.789-00"),
            "CPF não deve aparecer no motivo de erro (LGPD/CA99).");
        Assert.That(motivoErro, Does.Not.Contain("Joao Silva"),
            "Nome não deve aparecer no motivo de erro (LGPD/CA99).");
        // Deve ser uma das categorias fixas.
        var categoriasValidas = new[] { "limite_taxa_ia", "provider_indisponivel", "falha_classificacao" };
        Assert.That(categoriasValidas, Does.Contain(motivoErro),
            "Motivo de erro deve ser categoria genérica (CA99).");
    }

    // ─── CA97 — reprocessar parcial pula blocos OK ───────────────────────────

    [Test]
    public async Task ExecutarAsync_BlocoOkJaPersistido_NaoChama_IaNovamente()
    {
        // Bloco "pacientes" já tem mapa bem-sucedido persistido.
        var zip = CriarZipComCsv(
            ("pacientes.csv", "nome\nJoao\n"),
            ("agendamentos.csv", "data\n2024-01-01\n")
        );

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // "pacientes.csv" → bloco "pacientes" → já tem mapa OK (bloco_com_erro = false).
        var mapaOk = MigracaoMapa.Criar(JobId, EstabelecimentoId, EntidadesCanônicas.Paciente,
            """{"de_para":{"nome":"nome"},"confianca":0.9,"duvidas":[],"entidade_classificada":"paciente","confianca_classificacao":0.9,"ignorado":false,"encoding_suspeito":false}""",
            "pacientes");
        _mapaRepo
            .Setup(r => r.ObterPorJobBlocoOuNulo(
                JobId, "pacientes", EstabelecimentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapaOk);

        var chamadasMapeador = 0;
        _mapeador
            .Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => chamadasMapeador++)
            .ReturnsAsync(PropostaSucesso);

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // Apenas 1 chamada de IA — para "agendamentos" (não para "pacientes" que já está OK).
        Assert.That(chamadasMapeador, Is.EqualTo(1),
            "Bloco OK já persistido não deve gerar nova chamada de IA (CA97/R-R8).");
    }

    // ─── CA98 — reprocessar bloco de erro rechama a IA ───────────────────────

    [Test]
    public async Task ExecutarAsync_BlocoComErro_ChamamIA_Novamente()
    {
        var zip = CriarZipComCsv(("pacientes.csv", "nome\nJoao\n"));

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Bloco "pacientes" tem mapa de ERRO (bloco_com_erro = true).
        var mapaErro = MigracaoMapa.Criar(JobId, EstabelecimentoId, EntidadesCanônicas.SemEquivalente,
            """{"de_para":{},"confianca":0.0,"duvidas":[],"entidade_classificada":"sem_equivalente","confianca_classificacao":0.0,"ignorado":false,"encoding_suspeito":false,"bloco_com_erro":true,"motivo_erro":"limite_taxa_ia"}""",
            "pacientes");
        _mapaRepo
            .Setup(r => r.ObterPorJobBlocoOuNulo(
                JobId, "pacientes", EstabelecimentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapaErro);

        var chamadasMapeador = 0;
        _mapeador
            .Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => chamadasMapeador++)
            .ReturnsAsync(PropostaSucesso);

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // Bloco de ERRO deve ser re-inferido (CA98).
        Assert.That(chamadasMapeador, Is.EqualTo(1),
            "Bloco com erro deve gerar nova chamada de IA ao reprocessar (CA98).");
    }

    // ─── CA90 — pausa fixa configurável entre blocos ──────────────────────────

    [Test]
    public async Task ExecutarAsync_PausaConfiguravel_EntreBlocos()
    {
        // Testa que o handler aceita configuração de pausa sem falhar.
        // Usamos 0ms para não travar; o teste valida apenas que o fluxo segue correto.
        var zip = CriarZipComCsv(
            ("a.csv", "nome\nJoao\n"),
            ("b.csv", "data\n2024-01-01\n")
        );

        var job = CriarJobAguardandoMapa();
        _jobRepo.Setup(r => r.ObterMaisAntigoAguardandoMapaOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zip);
        _mapeador
            .Setup(m => m.InferirBlocoAsync(
                It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PropostaSucesso);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var parser = new Imedto.Backend.Infrastructure.Migracao.CsvMigracaoParser();
        var sut = CriarSut(parser);

        await sut.ExecutarAsync(CancellationToken.None);

        // 2 blocos = 2 chamadas ao mapeador (CA90: espaçamento não muda o número de chamadas).
        _mapeador.Verify(m => m.InferirBlocoAsync(
            It.IsAny<EsquemaDeArquivo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2), "Deve processar os 2 blocos sequencialmente (CA90).");
    }
}
