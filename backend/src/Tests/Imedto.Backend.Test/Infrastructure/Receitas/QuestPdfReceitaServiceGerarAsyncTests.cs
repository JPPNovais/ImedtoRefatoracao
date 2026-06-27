using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Receitas;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Receitas;

/// <summary>
/// Testes de unidade para <see cref="QuestPdfReceitaService.GerarAsync"/>:
/// validação de Rascunho (CA2), audit LGPD (CA4/CA5/CA6), receita não encontrada (CA7).
///
/// Usa subclasse testável que sobrescreve <c>CarregarDadosAsync</c> para eliminar
/// a dependência de banco (Dapper). Os testes de geração do PDF em si estão em
/// <see cref="QuestPdfReceitaServiceTests"/>.
/// </summary>
[TestFixture]
public class QuestPdfReceitaServiceGerarAsyncTests
{
    private const long EstabId = 42;
    private const long PacienteId = 99;
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private Mock<IProntuarioRepository> _prontuarioRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private TestableQuestPdfReceitaService _sut;

    [SetUp]
    public void Setup()
    {
        QuestPdfReceitaService.InicializarQuestPdf();

        _prontuarioRepo = new Mock<IProntuarioRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();

        _sut = new TestableQuestPdfReceitaService(
            _prontuarioRepo.Object,
            _acessoLog.Object);
    }

    // ── CA2: Rascunho → 422 ────────────────────────────────────────────────────

    [Test]
    public void GerarAsync_StatusRascunho_LancaBusinessException()
    {
        _sut.DadosFixos = DadosReceita(status: "Rascunho");

        var ex = Assert.ThrowsAsync<BusinessException>(
            () => _sut.GerarAsync(100, EstabId, UsuarioId, TenantPapel.Profissional));

        Assert.That(ex!.Message, Does.Contain("rascunho").IgnoreCase);
    }

    [Test]
    public void GerarAsync_StatusRascunho_NaoRegistraAudit()
    {
        _sut.DadosFixos = DadosReceita(status: "Rascunho");

        Assert.ThrowsAsync<BusinessException>(
            () => _sut.GerarAsync(100, EstabId, UsuarioId, TenantPapel.Profissional));

        _acessoLog.Verify(
            s => s.RegistrarAsync(It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()),
            Times.Never);
    }

    // ── CA4: Audit de Exportacao para receita emitida com prontuário ──────────

    [Test]
    public async Task GerarAsync_EmitidaComProntuario_RegistraAuditExportacao()
    {
        _sut.DadosFixos = DadosReceita(status: "Emitida");

        var prontuario = Prontuario.Iniciar(PacienteId, EstabId, 1L);
        _prontuarioRepo
            .Setup(r => r.ObterPorPaciente(PacienteId, EstabId))
            .ReturnsAsync(prontuario);
        _acessoLog
            .Setup(s => s.RegistrarAsync(It.IsAny<long>(), UsuarioId, EstabId, TipoAcessoProntuario.Exportacao))
            .Returns(Task.CompletedTask);

        var bytes = await _sut.GerarAsync(100, EstabId, UsuarioId, TenantPapel.Profissional);

        Assert.That(bytes, Is.Not.Null.And.Not.Empty);
        _acessoLog.Verify(
            s => s.RegistrarAsync(prontuario.Id, UsuarioId, EstabId, TipoAcessoProntuario.Exportacao),
            Times.Once,
            "Deve registrar exatamente uma linha de audit de Exportacao.");
    }

    // ── CA5: Sem prontuário → sem audit, download normal ──────────────────────

    [Test]
    public async Task GerarAsync_EmitidaSemProntuario_DownloadConclui_SemAudit()
    {
        _sut.DadosFixos = DadosReceita(status: "Emitida");

        _prontuarioRepo
            .Setup(r => r.ObterPorPaciente(PacienteId, EstabId))
            .ReturnsAsync((Prontuario)null);

        var bytes = await _sut.GerarAsync(100, EstabId, UsuarioId, TenantPapel.Profissional);

        Assert.That(bytes, Is.Not.Null.And.Not.Empty, "PDF deve ser gerado mesmo sem prontuário.");
        _acessoLog.Verify(
            s => s.RegistrarAsync(It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()),
            Times.Never,
            "Sem prontuário nenhuma linha de audit deve ser inserida.");
    }

    // ── CA6: Falha no audit não bloqueia o download ───────────────────────────

    [Test]
    public async Task GerarAsync_FalhaAudit_DownloadNaoBloqueado()
    {
        _sut.DadosFixos = DadosReceita(status: "Emitida");

        var prontuario = Prontuario.Iniciar(PacienteId, EstabId, 1L);
        _prontuarioRepo
            .Setup(r => r.ObterPorPaciente(PacienteId, EstabId))
            .ReturnsAsync(prontuario);
        _acessoLog
            .Setup(s => s.RegistrarAsync(It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()))
            .ThrowsAsync(new InvalidOperationException("Banco indisponível"));

        // Não deve lançar (best-effort — CA6).
        var bytes = await _sut.GerarAsync(100, EstabId, UsuarioId, TenantPapel.Profissional);

        Assert.That(bytes, Is.Not.Null.And.Not.Empty);
    }

    // ── CA7: Receita não encontrada (multi-tenant miss) ───────────────────────

    [Test]
    public void GerarAsync_ReceitaNaoEncontrada_LancaBusinessException()
    {
        _sut.DadosFixos = null; // simula query sem resultado (outra tenant ou inexistente)

        var ex = Assert.ThrowsAsync<BusinessException>(
            () => _sut.GerarAsync(100, EstabId, UsuarioId, TenantPapel.Profissional));

        Assert.That(ex!.Message, Does.Contain("não encontrada").IgnoreCase);
    }

    [Test]
    public void GerarAsync_ReceitaNaoEncontrada_NaoRegistraAudit()
    {
        _sut.DadosFixos = null;

        Assert.ThrowsAsync<BusinessException>(
            () => _sut.GerarAsync(100, EstabId, UsuarioId, TenantPapel.Profissional));

        _acessoLog.Verify(
            s => s.RegistrarAsync(It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()),
            Times.Never);
    }

    // ── CA3: Cancelada continua gerando PDF (não regrediu) ────────────────────

    [Test]
    public async Task GerarAsync_StatusCancelada_GeraComMarcaDagua()
    {
        _sut.DadosFixos = DadosReceita(status: "Cancelada");
        _prontuarioRepo
            .Setup(r => r.ObterPorPaciente(PacienteId, EstabId))
            .ReturnsAsync(Prontuario.Iniciar(PacienteId, EstabId, 1L));

        var bytes = await _sut.GerarAsync(100, EstabId, UsuarioId, TenantPapel.Profissional);

        Assert.That(bytes, Is.Not.Null.And.Not.Empty, "Cancelada deve continuar gerando PDF.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DadosPdf DadosReceita(string status) =>
        new(
            new ReceitaRow(
                Id: 100,
                Tipo: "Comum",
                TipoNotificacao: null,
                Status: status,
                AssinaturaDigitalStatus: "NaoAssinada",
                EmitidaEm: status == "Rascunho" ? null : new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc),
                ValidadeAte: null,
                Observacoes: null,
                MotivoCancelamento: null,
                PacienteId: PacienteId,
                PacienteNome: "João da Silva",
                PacienteCpf: "12345678901",
                PacienteDataNascimento: new DateTime(1980, 1, 1),
                PacienteGenero: "M",
                PacienteTelefone: null,
                ProfissionalNome: "Dr. Ana",
                ProfissionalCrmCro: "CRM SP 99999",
                CabecalhoHtml: null,
                RodapeHtml: null,
                EmissorPadrao: null,
                EstabelecimentoNomeFantasia: "Clínica Teste",
                EstabelecimentoCnpj: null,
                EstabelecimentoTelefone: null,
                EstabelecimentoEndereco: null,
                EstabelecimentoFotoUrl: null,
                ProfissionalUsuarioId: UsuarioId),
            new List<ItemRow>
            {
                new(Ordem: 0, Medicamento: "Dipirona", Posologia: "1 cp 8/8h",
                    Concentracao: null, FormaFarmaceutica: null, Via: null,
                    Quantidade: null, Duracao: null, Observacao: null)
            });

    // ── Subclasse testável — sobrescreve a query Dapper ───────────────────────

    private sealed class TestableQuestPdfReceitaService : QuestPdfReceitaService
    {
        public DadosPdf DadosFixos { get; set; }

        public TestableQuestPdfReceitaService(
            IProntuarioRepository prontuarioRepo,
            IProntuarioAcessoLogService acessoLog)
            : base(
                new AppReadConnectionString("Host=localhost;"),
                new TestHttpClientFactory(),
                prontuarioRepo,
                acessoLog,
                NullLogger<QuestPdfReceitaService>.Instance)
        { }

        internal override Task<DadosPdf> CarregarDadosAsync(long receitaId, long estabelecimentoId)
            => Task.FromResult(DadosFixos);
    }

    private sealed class TestHttpClientFactory : System.Net.Http.IHttpClientFactory
    {
        public System.Net.Http.HttpClient CreateClient(string name) => new();
    }
}
