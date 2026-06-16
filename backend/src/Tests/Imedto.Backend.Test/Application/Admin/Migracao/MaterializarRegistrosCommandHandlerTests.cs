using System.Text.Json;
using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Admin.Migracao;

/// <summary>
/// Testes da etapa de materialização de registros (addendum 6 — CA102–CA115).
/// Cobrem: materialização por linha de bloco aceito (CA102), payload canônico (CA103),
/// dump aninhado N entidades (CA106), blocos não-materializáveis (CA107),
/// coluna ignorar descartada e valor inteiro (CA108), obrigatório ausente = cria pendente (CA109),
/// idempotência re-materializar — limpa pendentes, preserva importado_* (CA110),
/// multi-tenant (CA111), degradação bloco_com_erro (CA115), Onda 2 só prontuário (CA114).
/// </summary>
[TestFixture]
public class MaterializarRegistrosCommandHandlerTests
{
    private Mock<IMigracaoJobRepository>          _jobRepo;
    private Mock<IMigracaoMapaRepository>         _mapaRepo;
    private Mock<IMigracaoRegistroRepository>     _registroRepo;
    private Mock<IMigracaoArquivoStorageService>  _storage;
    private Mock<IMigracaoArquivoParser>          _parser;

    private MaterializarRegistrosCommandHandler _sut;

    private const long EstabelecimentoId = 42;
    private const long JobId = 99;
    private static readonly Guid AdminId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _jobRepo      = new Mock<IMigracaoJobRepository>();
        _mapaRepo     = new Mock<IMigracaoMapaRepository>();
        _registroRepo = new Mock<IMigracaoRegistroRepository>();
        _storage      = new Mock<IMigracaoArquivoStorageService>();
        _parser       = new Mock<IMigracaoArquivoParser>();

        _registroRepo.Setup(r => r.DeletarPendentesPorJob(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        _registroRepo.Setup(r => r.SalvarLote(It.IsAny<IReadOnlyList<MigracaoRegistro>>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

        _sut = new MaterializarRegistrosCommandHandler(
            _jobRepo.Object,
            _mapaRepo.Object,
            _registroRepo.Object,
            _storage.Object,
            new[] { _parser.Object },
            NullLogger<MaterializarRegistrosCommandHandler>.Instance);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private MigracaoJob CriarJobEmRevisao()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, Guid.NewGuid(), "iClinic");
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        job.RegistrarArquivoRecebido("migracao/42/99/arquivo.zip");
        job.AprovarAnalise(AdminId);
        job.MarcarMapaEmRevisao();
        return job;
    }

    private MigracaoMapa CriarMapa(
        string entidade,
        Dictionary<string, string> dePara,
        string nomeBlocoOrigem = "",
        bool ignorado = false,
        bool ehConfig = false,
        bool blocoComErro = false)
    {
        var mapaObj = new Dictionary<string, object>
        {
            ["de_para"]     = dePara,
            ["ignorado"]    = ignorado,
            ["eh_config"]   = ehConfig,
        };
        if (blocoComErro) mapaObj["bloco_com_erro"] = true;

        var mapaJson = JsonSerializer.Serialize(mapaObj);
        var mapa = MigracaoMapa.Criar(JobId, EstabelecimentoId, entidade, mapaJson, nomeBlocoOrigem);
        return mapa;
    }

    private void ConfigurarParserComBlocos(params BlocoCandidato[] blocos)
    {
        _parser.Setup(p => p.SuportaFormato(It.IsAny<string>()))
               .Returns(true);

        var linhasPlanas = blocos.SelectMany(b => b.Linhas).ToList();
        var parseado = new ArquivoParseado
        {
            Cabecalhos = blocos.FirstOrDefault()?.Cabecalhos ?? [],
            Linhas     = linhasPlanas,
            Blocos     = blocos,
        };
        _parser.Setup(p => p.ParsearAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(parseado);

        // ZIP com 1 entrada (nome fixo para o parser reconhecer)
        var zipStream = CriarZipComEntrada("dados.json", "{}"u8.ToArray());
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(zipStream);
    }

    private static Stream CriarZipComEntrada(string nome, byte[] conteudo)
    {
        var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry(nome);
            using var entryStream = entry.Open();
            entryStream.Write(conteudo);
        }
        ms.Position = 0;
        return ms;
    }

    private static BlocoCandidato CriarBloco(string nome, IReadOnlyList<IReadOnlyDictionary<string, string>> linhas)
        => new()
        {
            NomeBloco   = nome,
            Cabecalhos  = linhas.SelectMany(l => l.Keys).Distinct().ToArray(),
            Linhas      = linhas,
            EhConfig    = false,
        };

    // ─── CA102 — 1 registro pendente por linha de bloco aceito ───────────────────

    [Test]
    public async Task CA102_MaterializacaoCria1RegistroPorLinha()
    {
        // Arrange
        var job = CriarJobEmRevisao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);

        var linhas = Enumerable.Range(1, 30)
            .Select(i => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>
                { ["nome_cliente"] = $"Paciente {i}", ["doc"] = $"CPF-{i}" })
            .ToList();

        var bloco = CriarBloco("pacientes", linhas);
        var mapa = CriarMapa(
            "paciente",
            new Dictionary<string, string> { ["nome_cliente"] = "nome_completo", ["doc"] = "cpf" },
            nomeBlocoOrigem: "pacientes");

        ConfigurarParserComBlocos(bloco);
        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, default))
                 .ReturnsAsync(new List<MigracaoMapa> { mapa });

        IReadOnlyList<MigracaoRegistro>? registrosSalvos = null;
        _registroRepo.Setup(r => r.SalvarLote(It.IsAny<IReadOnlyList<MigracaoRegistro>>(), default))
                     .Callback<IReadOnlyList<MigracaoRegistro>, CancellationToken>((lista, _) => registrosSalvos = lista)
                     .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(JobId, default);

        // Assert
        Assert.That(registrosSalvos, Is.Not.Null);
        Assert.That(registrosSalvos!.Count, Is.EqualTo(30), "CA102: deve criar 1 registro por linha");
        Assert.That(registrosSalvos.All(r => r.Entidade == "paciente"), Is.True);
        Assert.That(registrosSalvos.All(r => r.EstabelecimentoId == EstabelecimentoId), Is.True);
        Assert.That(registrosSalvos.All(r => r.Status == "pendente"), Is.True);
    }

    // ─── CA103 — payload com chaves canônicas ────────────────────────────────────

    [Test]
    public async Task CA103_PayloadMapeadoParaCamposCanonicos()
    {
        // Arrange
        var job = CriarJobEmRevisao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);

        var linhas = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                ["nome_completo_cliente"] = "Maria Oliveira",
                ["documento"]            = "123.456.789-00",
                ["ignorar_campo"]        = "dados internos",
            }
        };
        var bloco = CriarBloco("clientes", linhas);
        // de_para: nome_completo_cliente → nome_completo, documento → cpf, ignorar_campo → ignorar
        var mapa = CriarMapa(
            "paciente",
            new Dictionary<string, string>
            {
                ["nome_completo_cliente"] = "nome_completo",
                ["documento"]            = "cpf",
                ["ignorar_campo"]        = "ignorar",  // CA108: deve ser descartado
            },
            nomeBlocoOrigem: "clientes");

        ConfigurarParserComBlocos(bloco);
        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, default))
                 .ReturnsAsync(new List<MigracaoMapa> { mapa });

        IReadOnlyList<MigracaoRegistro>? registrosSalvos = null;
        _registroRepo.Setup(r => r.SalvarLote(It.IsAny<IReadOnlyList<MigracaoRegistro>>(), default))
                     .Callback<IReadOnlyList<MigracaoRegistro>, CancellationToken>((lista, _) => registrosSalvos = lista)
                     .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(JobId, default);

        // Assert
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(registrosSalvos![0].PayloadBruto)!;

        Assert.That(payload.ContainsKey("nome_completo"), Is.True, "CA103: chave canônica nome_completo deve estar no payload");
        Assert.That(payload["nome_completo"], Is.EqualTo("Maria Oliveira"));
        Assert.That(payload.ContainsKey("cpf"), Is.True, "CA103: chave canônica cpf deve estar no payload");
        Assert.That(payload["cpf"], Is.EqualTo("123.456.789-00"));
        // CA108: coluna ignorar deve ser descartada
        Assert.That(payload.ContainsKey("ignorar_campo"), Is.False, "CA108: coluna marcada ignorar não deve aparecer no payload");
    }

    // ─── CA106 — dump aninhado materializa N entidades ───────────────────────────

    [Test]
    public async Task CA106_DumpAninhadoMaterializaNEntidades()
    {
        // Arrange
        var job = CriarJobEmRevisao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);

        var linhaPaciente = new Dictionary<string, string> { ["nome"] = "Ana", ["cpf"] = "111" };
        var linhaAgenda   = new Dictionary<string, string> { ["paciente_cpf"] = "111", ["data"] = "2026-01-10" };

        var blocoPacientes     = CriarBloco("pacientes",   new[] { (IReadOnlyDictionary<string, string>)linhaPaciente });
        var blocoConsultas     = CriarBloco("consultas",   new[] { (IReadOnlyDictionary<string, string>)linhaAgenda });

        ConfigurarParserComBlocos(blocoPacientes, blocoConsultas);

        var mapaPaciente    = CriarMapa("paciente",     new Dictionary<string, string> { ["nome"] = "nome_completo", ["cpf"] = "cpf" },     nomeBlocoOrigem: "pacientes");
        var mapaAgendamento = CriarMapa("agendamento",  new Dictionary<string, string> { ["paciente_cpf"] = "cpf_paciente", ["data"] = "inicio_previsto" }, nomeBlocoOrigem: "consultas");

        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, default))
                 .ReturnsAsync(new List<MigracaoMapa> { mapaPaciente, mapaAgendamento });

        IReadOnlyList<MigracaoRegistro>? registrosSalvos = null;
        _registroRepo.Setup(r => r.SalvarLote(It.IsAny<IReadOnlyList<MigracaoRegistro>>(), default))
                     .Callback<IReadOnlyList<MigracaoRegistro>, CancellationToken>((lista, _) => registrosSalvos = lista)
                     .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(JobId, default);

        // Assert
        Assert.That(registrosSalvos, Is.Not.Null);
        Assert.That(registrosSalvos!.Count, Is.EqualTo(2), "CA106: 1 paciente + 1 agendamento");
        Assert.That(registrosSalvos.Any(r => r.Entidade == "paciente"), Is.True, "CA106: deve ter paciente");
        Assert.That(registrosSalvos.Any(r => r.Entidade == "agendamento"), Is.True, "CA106: deve ter agendamento");
    }

    // ─── CA107 — blocos não-materializáveis não geram registro ──────────────────

    [Test]
    public async Task CA107_BlocosNaoMaterializaveisNaoGeramRegistro()
    {
        // Arrange
        var job = CriarJobEmRevisao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);

        var linhas = new[] { (IReadOnlyDictionary<string, string>)new Dictionary<string, string> { ["x"] = "y" } };
        var blocoNormal = CriarBloco("pacientes", linhas);

        ConfigurarParserComBlocos(blocoNormal);

        // 3 mapas não-materializáveis
        var mapaSemEquivalente = CriarMapa("sem_equivalente", new Dictionary<string, string> { ["x"] = "y" }, "config_bloco");
        var mapaIgnorado      = CriarMapa("paciente",         new Dictionary<string, string> { ["x"] = "y" }, "lixo", ignorado: true);
        var mapaEhConfig      = CriarMapa("paciente",         new Dictionary<string, string> { ["x"] = "y" }, "estabelecimento", ehConfig: true);
        // 1 mapa válido
        var mapaValido        = CriarMapa("paciente",         new Dictionary<string, string> { ["x"] = "nome_completo" }, "pacientes");

        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, default))
                 .ReturnsAsync(new List<MigracaoMapa> { mapaSemEquivalente, mapaIgnorado, mapaEhConfig, mapaValido });

        IReadOnlyList<MigracaoRegistro>? registrosSalvos = null;
        _registroRepo.Setup(r => r.SalvarLote(It.IsAny<IReadOnlyList<MigracaoRegistro>>(), default))
                     .Callback<IReadOnlyList<MigracaoRegistro>, CancellationToken>((lista, _) => registrosSalvos = lista)
                     .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(JobId, default);

        // Assert — somente o mapa válido gera registro (1 linha)
        Assert.That(registrosSalvos, Is.Not.Null);
        Assert.That(registrosSalvos!.Count, Is.EqualTo(1), "CA107: apenas o bloco aceito materializa");
        Assert.That(registrosSalvos[0].Entidade, Is.EqualTo("paciente"));
    }

    // ─── CA108 — valor real inteiro (nunca truncado) ─────────────────────────────

    [Test]
    public async Task CA108_ValorRealInteiroNuncaTruncado()
    {
        // Arrange
        var job = CriarJobEmRevisao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);

        var valorLongo = new string('A', 800); // 800 chars — acima do limite da IA (500)
        var linhas = new[] { (IReadOnlyDictionary<string, string>)new Dictionary<string, string> { ["nome_cliente"] = valorLongo } };
        var bloco = CriarBloco("pacientes", linhas);

        ConfigurarParserComBlocos(bloco);
        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, default))
                 .ReturnsAsync(new List<MigracaoMapa>
                 {
                     CriarMapa("paciente", new Dictionary<string, string> { ["nome_cliente"] = "nome_completo" }, "pacientes")
                 });

        IReadOnlyList<MigracaoRegistro>? registrosSalvos = null;
        _registroRepo.Setup(r => r.SalvarLote(It.IsAny<IReadOnlyList<MigracaoRegistro>>(), default))
                     .Callback<IReadOnlyList<MigracaoRegistro>, CancellationToken>((lista, _) => registrosSalvos = lista)
                     .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(JobId, default);

        // Assert — nome_completo deve ter os 800 chars inteiros, não 500
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(registrosSalvos![0].PayloadBruto)!;
        Assert.That(payload["nome_completo"].Length, Is.EqualTo(800), "CA108: valor real inteiro (800 chars), não truncado a 500");
        Assert.That(payload["nome_completo"], Does.Not.Contain("[truncado]"), "CA108: não deve ter marcador de truncamento");
    }

    // ─── CA109 — obrigatório ausente → registro criado na materialização; carga rejeita ──

    [Test]
    public async Task CA109_ObrigatorioAusente_RegistroCriadoPendente()
    {
        // Arrange
        var job = CriarJobEmRevisao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);

        // Linha sem CPF (campo obrigatório para paciente na carga)
        var linhas = new[] { (IReadOnlyDictionary<string, string>)new Dictionary<string, string> { ["nome"] = "Pedro Silva" } };
        var bloco = CriarBloco("pacientes", linhas);

        ConfigurarParserComBlocos(bloco);
        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, default))
                 .ReturnsAsync(new List<MigracaoMapa>
                 {
                     CriarMapa("paciente", new Dictionary<string, string> { ["nome"] = "nome_completo" /* cpf ausente */ }, "pacientes")
                 });

        IReadOnlyList<MigracaoRegistro>? registrosSalvos = null;
        _registroRepo.Setup(r => r.SalvarLote(It.IsAny<IReadOnlyList<MigracaoRegistro>>(), default))
                     .Callback<IReadOnlyList<MigracaoRegistro>, CancellationToken>((lista, _) => registrosSalvos = lista)
                     .Returns(Task.CompletedTask);

        // Act — materialização não lança exceção nem rejeita o registro
        await _sut.ExecutarAsync(JobId, default);

        // Assert — registro criado mesmo sem CPF (R-M6/CA109)
        Assert.That(registrosSalvos, Is.Not.Null);
        Assert.That(registrosSalvos!.Count, Is.EqualTo(1), "CA109: materialização cria o registro mesmo sem campo obrigatório");
        Assert.That(registrosSalvos[0].Status, Is.EqualTo("pendente"), "CA109: status pendente — carga decidirá");

        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(registrosSalvos[0].PayloadBruto)!;
        Assert.That(payload.ContainsKey("cpf"), Is.False, "CA109: cpf não estava no de-para, não deve aparecer");
        Assert.That(payload["nome_completo"], Is.EqualTo("Pedro Silva"));
    }

    // ─── CA110 — idempotência: re-materializar limpa pendentes, preserva importado_* ──

    [Test]
    public async Task CA110_ReMaterializar_LimpaPendentes_PreservaImportado()
    {
        // Arrange
        var job = CriarJobEmRevisao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);

        var linhas = new[] { (IReadOnlyDictionary<string, string>)new Dictionary<string, string> { ["nome"] = "Ana" } };
        var bloco = CriarBloco("pacientes", linhas);

        ConfigurarParserComBlocos(bloco);
        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, default))
                 .ReturnsAsync(new List<MigracaoMapa>
                 {
                     CriarMapa("paciente", new Dictionary<string, string> { ["nome"] = "nome_completo" }, "pacientes")
                 });

        _registroRepo.Setup(r => r.SalvarLote(It.IsAny<IReadOnlyList<MigracaoRegistro>>(), default))
                     .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(JobId, default);

        // Assert: DeletarPendentesPorJob foi chamado (idempotência)
        _registroRepo.Verify(
            r => r.DeletarPendentesPorJob(JobId, It.IsAny<CancellationToken>()),
            Times.Once,
            "CA110: re-materializar deve apagar pendentes antes de reger");

        // Garante que NÃO chamou nenhum método que afetaria importado_criado
        // (apenas DeletarPendentesPorJob + SalvarLote — sem update de outros status)
        _registroRepo.Verify(
            r => r.Salvar(It.IsAny<MigracaoRegistro>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "CA110: materialização não deve chamar Salvar individual (apenas SalvarLote para novos)");
    }

    // ─── CA111 — multi-tenant: estabelecimentoId herdado do job ─────────────────

    [Test]
    public async Task CA111_MultiTenant_EstabelecimentoIdHerdadoDoJob()
    {
        // Arrange
        var job = CriarJobEmRevisao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);

        var linhas = new[] { (IReadOnlyDictionary<string, string>)new Dictionary<string, string> { ["n"] = "x" } };
        var bloco = CriarBloco("pacientes", linhas);
        ConfigurarParserComBlocos(bloco);
        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, default))
                 .ReturnsAsync(new List<MigracaoMapa>
                 {
                     CriarMapa("paciente", new Dictionary<string, string> { ["n"] = "nome_completo" }, "pacientes")
                 });

        IReadOnlyList<MigracaoRegistro>? registrosSalvos = null;
        _registroRepo.Setup(r => r.SalvarLote(It.IsAny<IReadOnlyList<MigracaoRegistro>>(), default))
                     .Callback<IReadOnlyList<MigracaoRegistro>, CancellationToken>((lista, _) => registrosSalvos = lista)
                     .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(JobId, default);

        // Assert
        Assert.That(registrosSalvos!.All(r => r.EstabelecimentoId == EstabelecimentoId), Is.True,
            "CA111: todos os registros devem herdar EstabelecimentoId do job");
    }

    // ─── CA111 — falha-fechada: job não encontrado → BusinessException ───────────

    [Test]
    public async Task CA111_JobNaoEncontrado_LancaBusinessException()
    {
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync((MigracaoJob?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(
            () => _sut.ExecutarAsync(JobId, default));

        Assert.That(ex!.Message, Does.Contain("não encontrado").IgnoreCase);
    }

    // ─── CA115 — degradação: bloco_com_erro não materializa; OK materializa ─────

    [Test]
    public async Task CA115_BlocoComErroNaoMaterializa_BlocosOkMaterializam()
    {
        // Arrange
        var job = CriarJobEmRevisao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);

        var linhaOk = new Dictionary<string, string> { ["nome"] = "OK" };
        var blocoOk = CriarBloco("pacientes", new[] { (IReadOnlyDictionary<string, string>)linhaOk });
        ConfigurarParserComBlocos(blocoOk);

        var mapaComErro = CriarMapa("paciente", new Dictionary<string, string>(), "entradas", blocoComErro: true);
        var mapaOk1     = CriarMapa("paciente", new Dictionary<string, string> { ["nome"] = "nome_completo" }, "pacientes");
        var mapaOk2     = CriarMapa("paciente", new Dictionary<string, string> { ["nome"] = "nome_completo" }, "pacientes_novos");
        // Nota: pacientes_novos não existe nos blocos — bloco não encontrado → pulado silenciosamente

        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, default))
                 .ReturnsAsync(new List<MigracaoMapa> { mapaComErro, mapaOk1, mapaOk2 });

        IReadOnlyList<MigracaoRegistro>? registrosSalvos = null;
        _registroRepo.Setup(r => r.SalvarLote(It.IsAny<IReadOnlyList<MigracaoRegistro>>(), default))
                     .Callback<IReadOnlyList<MigracaoRegistro>, CancellationToken>((lista, _) => registrosSalvos = lista)
                     .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(JobId, default);

        // Assert — bloco com erro não gera registro; bloco OK (1 linha) gera 1 registro
        Assert.That(registrosSalvos, Is.Not.Null);
        Assert.That(registrosSalvos!.Count, Is.EqualTo(1), "CA115: bloco_com_erro não materializa; bloco OK sim");
        Assert.That(registrosSalvos[0].Entidade, Is.EqualTo("paciente"));
    }

    // ─── CA114 — sem mapas → nenhum registro, sem exceção ───────────────────────

    [Test]
    public async Task SemMapas_NenhumRegistroMaterializado()
    {
        var job = CriarJobEmRevisao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);
        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, default))
                 .ReturnsAsync(new List<MigracaoMapa>());

        // Act — não deve lançar
        await _sut.ExecutarAsync(JobId, default);

        // Assert — SalvarLote nunca chamado quando não há mapas
        _registroRepo.Verify(
            r => r.SalvarLote(It.IsAny<IReadOnlyList<MigracaoRegistro>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── Status errado → BusinessException ───────────────────────────────────────

    [Test]
    public async Task StatusErrado_LancaBusinessException()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, Guid.NewGuid());
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        job.RegistrarArquivoRecebido("key");
        // Não avança para mapa_em_revisao → status = aguardando_aprovacao

        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, default)).ReturnsAsync(job);

        Assert.ThrowsAsync<BusinessException>(() => _sut.ExecutarAsync(JobId, default));
    }
}
