using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Prontuarios;

/// <summary>
/// Testes de unidade do QuestPdfProntuarioService.
/// Cobre: multi-tenant falha-fechada (outro tenant → 422 genérico),
/// audit registrado na emissão bem-sucedida, e audit best-effort (falha não bloqueia).
///
/// Usa subclasse testável que sobrescreve CarregarDadosAsync para eliminar dependência de banco.
/// </summary>
[TestFixture]
public class QuestPdfProntuarioServiceTests
{
    private const long PacienteId = 10L;
    private const long EstabId = 1L;
    private const long OutroEstabId = 2L;
    private const long ProntuarioId = 999L;
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private Mock<IProntuarioRepository> _prontuarioRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private TestableProntuarioPdfService _sut;

    [SetUp]
    public void SetUp()
    {
        _prontuarioRepo = new Mock<IProntuarioRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _acessoLog.Setup(a => a.RegistrarAsync(
            It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()))
            .Returns(Task.CompletedTask);

        _sut = new TestableProntuarioPdfService(_prontuarioRepo.Object, _acessoLog.Object);
    }

    // ── Multi-tenant: prontuário de outro tenant → 422 genérico ──────────────

    [Test]
    public void GerarAsync_ProntuarioCrossTenant_LancaMensagemGenerica()
    {
        // CarregarDadosAsync retorna null quando o prontuário pertence a outro tenant
        // (query filtra por estabelecimento_id — falha-fechada).
        _sut.DadosFixos = null;

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.GerarAsync(PacienteId, OutroEstabId, UsuarioId));

        Assert.That(ex!.Message, Is.EqualTo("Prontuário não encontrado."),
            "Mensagem genérica — não deve revelar que o prontuário pertence a outro tenant.");
    }

    [Test]
    public void GerarAsync_ProntuarioInexistente_LancaMensagemGenerica()
    {
        _sut.DadosFixos = null;

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.GerarAsync(PacienteId, EstabId, UsuarioId));

        Assert.That(ex!.Message, Is.EqualTo("Prontuário não encontrado."));
    }

    // ── Audit LGPD: registrado na emissão bem-sucedida ───────────────────────

    [Test]
    public async Task GerarAsync_ProntuarioEncontrado_RegistraAuditExportacao()
    {
        _sut.DadosFixos = DadosPadrao();

        await _sut.GerarAsync(PacienteId, EstabId, UsuarioId);

        _acessoLog.Verify(a => a.RegistrarAsync(
            ProntuarioId, UsuarioId, EstabId, TipoAcessoProntuario.Exportacao), Times.Once,
            "Audit de Exportacao deve ser registrado em toda emissão de PDF.");
    }

    // ── Audit best-effort: falha não bloqueia download ───────────────────────

    [Test]
    public async Task GerarAsync_FalhaAudit_NaoBloqueiaPdf()
    {
        _sut.DadosFixos = DadosPadrao();

        _acessoLog.Setup(a => a.RegistrarAsync(
            It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()))
            .ThrowsAsync(new InvalidOperationException("Banco indisponível"));

        // Não deve lançar — audit é best-effort.
        var bytes = await _sut.GerarAsync(PacienteId, EstabId, UsuarioId);
        Assert.That(bytes, Is.Not.Null.And.Not.Empty, "PDF deve ser retornado mesmo com falha de audit.");
    }

    // ── Emissão bem-sucedida retorna bytes não-vazios ────────────────────────

    [Test]
    public async Task GerarAsync_ProntuarioValido_RetornaBytesNaoVazios()
    {
        _sut.DadosFixos = DadosPadrao();

        var bytes = await _sut.GerarAsync(PacienteId, EstabId, UsuarioId);

        Assert.That(bytes, Is.Not.Null.And.Not.Empty);
    }

    // ── ExtrairTextoLegivel: cobre casos de conteúdo JSON ────────────────────

    [Test]
    public void ExtrairTextoLegivel_JsonSimples_RetornaTexto()
    {
        var conteudo = """{"queixa": "Dor de cabeça", "diagnostico": "Enxaqueca"}""";

        var texto = QuestPdfProntuarioService.ExtrairTextoLegivel(conteudo, null);

        Assert.That(texto, Is.Not.Null.And.Not.Empty);
        Assert.That(texto, Does.Contain("Dor de cabeça"));
        Assert.That(texto, Does.Contain("Enxaqueca"));
    }

    [Test]
    public void ExtrairTextoLegivel_JsonNulo_RetornaNulo()
    {
        Assert.That(QuestPdfProntuarioService.ExtrairTextoLegivel(null, null), Is.Null);
    }

    [Test]
    public void ExtrairTextoLegivel_JsonMalformado_RetornaNulo()
    {
        Assert.That(QuestPdfProntuarioService.ExtrairTextoLegivel("{invalido", null), Is.Null);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DadosProntuarioPdf DadosPadrao() => new(
        Cabecalho: new ProntuarioCabecalhoRow(
            ProntuarioId: ProntuarioId,
            PacienteId: PacienteId,
            PacienteNome: "João da Silva",
            PacienteDataNascimento: new DateTime(1990, 1, 1),
            PacienteGenero: "M",
            EstabelecimentoNomeFantasia: "Clínica Teste",
            EstabelecimentoCnpj: null,
            EstabelecimentoTelefone: null,
            EstabelecimentoEndereco: null,
            EstabelecimentoFotoUrl: null,
            ProntuarioCriadoEm: new DateTime(2024, 1, 15)),
        Evolucoes: new List<EvolucaoPdfRow>
        {
            new(Id: 1L,
                CriadaEm: new DateTime(2024, 3, 10, 10, 0, 0),
                AutorNome: "Dr. Teste",
                ModeloNome: "Consulta Geral",
                ConteudoJson: """{"queixa": "Dor de cabeça"}""",
                ModeloSnapshotJson: null,
                ContagemAnexos: 0)
        });

    // ── Subclasse testável — sobrescreve a query Dapper ───────────────────────

    private sealed class TestableProntuarioPdfService : QuestPdfProntuarioService
    {
        public DadosProntuarioPdf DadosFixos { get; set; }

        public TestableProntuarioPdfService(
            IProntuarioRepository prontuarioRepo,
            IProntuarioAcessoLogService acessoLog)
            : base(
                new AppReadConnectionString("Host=localhost;"),
                new TestHttpClientFactory(),
                prontuarioRepo,
                acessoLog,
                NullLogger<QuestPdfProntuarioService>.Instance)
        { }

        internal override Task<DadosProntuarioPdf> CarregarDadosAsync(long pacienteId, long estabelecimentoId)
            => Task.FromResult(DadosFixos);
    }

    private sealed class TestHttpClientFactory : System.Net.Http.IHttpClientFactory
    {
        public System.Net.Http.HttpClient CreateClient(string name) => new();
    }
}
