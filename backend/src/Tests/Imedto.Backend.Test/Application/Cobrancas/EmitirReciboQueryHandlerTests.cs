using Imedto.Backend.Application.Cobrancas.Queries;
using Imedto.Backend.Contracts.Cobrancas.Queries;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Cobrancas;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.Test.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Cobrancas;

/// <summary>
/// Testes do handler/serviço de emissão de recibo (F8).
/// Cobre: CA120 (estornado → 422), CA121 (não encontrado → 422),
/// CA124 (multi-tenant falha-fechada), CA127 (audit best-effort), CA128 (flag 1ª emissão).
///
/// Usa subclasse testável que sobrescreve CarregarDadosAsync para eliminar dependência de banco.
/// </summary>
[TestFixture]
public class EmitirReciboQueryHandlerTests
{
    private const long EstabId = 1L;
    private const long PacienteId = 10L;
    private const long CobrancaId = 99L;
    private const long PagamentoId = 20L;
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    private Mock<ICobrancaRepository> _cobrancaRepo;
    private Mock<IPacienteAcessoLogService> _acessoLog;
    private TestableQuestPdfReciboPagamentoService _pdfService;
    private EmitirReciboPagamentoQueryHandler _sut;

    [SetUp]
    public void SetUp()
    {
        _cobrancaRepo = new Mock<ICobrancaRepository>();
        _acessoLog = new Mock<IPacienteAcessoLogService>();
        _acessoLog.Setup(a => a.RegistrarAsync(It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoPaciente>(), null))
            .Returns(Task.CompletedTask);

        _pdfService = new TestableQuestPdfReciboPagamentoService(
            _cobrancaRepo.Object, _acessoLog.Object);

        _sut = new EmitirReciboPagamentoQueryHandler(_pdfService);
    }

    private Cobranca CriarCobrancaComPagamento()
    {
        var c = Cobranca.CriarParaConsulta(EstabId, PacienteId, 500L,
            TipoAtendimento.Particular, 200m, "Consulta", UsuarioId);
        c.SimularIdBanco(CobrancaId);
        c.RegistrarPagamento(200m, 1L, 1, 0m, 0m, Hoje, UsuarioId);
        c.Pagamentos.First().SimularIdBanco(PagamentoId);
        return c;
    }

    private EmitirReciboPagamentoQuery CriarQuery() => new()
    {
        PagamentoId = PagamentoId,
        EstabelecimentoId = EstabId,
        UsuarioId = UsuarioId,
    };

    // ── CA120: pagamento estornado → 422 ─────────────────────────────────────

    [Test]
    public void Handle_PagamentoEstornado_LancaBusinessException()
    {
        var cobranca = CriarCobrancaComPagamento();
        cobranca.EstornarPagamento(PagamentoId, "motivo", UsuarioId);

        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(cobranca);
        _pdfService.DadosFixos = DadosPadrao();

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(CriarQuery()));
        Assert.That(ex!.Message, Does.Contain("estornado").IgnoreCase);
    }

    // ── CA121: pagamento não encontrado → 422 genérico ───────────────────────

    [Test]
    public void Handle_DadosNaoEncontrados_LancaGenerico()
    {
        _pdfService.DadosFixos = null; // simula pagamento inexistente ou de outro tenant

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(CriarQuery()));
        Assert.That(ex!.Message, Does.Contain("Não encontrado").IgnoreCase);
    }

    // ── CA124: cobrança de outro tenant → 422 genérico ───────────────────────

    [Test]
    public void Handle_CobrancaDeOutroTenant_LancaGenerico()
    {
        _pdfService.DadosFixos = DadosPadrao();
        // Cobranca retorna null (outro tenant ou inexistente)
        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync((Cobranca)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(CriarQuery()));
        Assert.That(ex!.Message, Does.Contain("Não encontrado").IgnoreCase);
    }

    // ── CA127: audit registrado na emissão bem-sucedida ──────────────────────

    [Test]
    public async Task Handle_EmissaoBemSucedida_RegistraAuditLeitura()
    {
        var cobranca = CriarCobrancaComPagamento();
        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(cobranca);
        _cobrancaRepo.Setup(r => r.Salvar(cobranca)).Returns(Task.CompletedTask);
        _pdfService.DadosFixos = DadosPadrao();

        var bytes = await _sut.Handle(CriarQuery());

        Assert.That(bytes, Is.Not.Null.And.Not.Empty, "PDF deve ser retornado.");
        _acessoLog.Verify(a => a.RegistrarAsync(
            PacienteId, UsuarioId, EstabId, TipoAcessoPaciente.Leitura, null), Times.Once);
    }

    // ── CA127: falha no audit não bloqueia a emissão (best-effort) ───────────

    [Test]
    public async Task Handle_FalhaAudit_NaoBloqueia()
    {
        var cobranca = CriarCobrancaComPagamento();
        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(cobranca);
        _cobrancaRepo.Setup(r => r.Salvar(cobranca)).Returns(Task.CompletedTask);
        _pdfService.DadosFixos = DadosPadrao();

        _acessoLog.Setup(a => a.RegistrarAsync(It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(),
            It.IsAny<TipoAcessoPaciente>(), null))
            .ThrowsAsync(new InvalidOperationException("Banco indisponível"));

        // Não deve lançar — audit é best-effort (CA127)
        var bytes = await _sut.Handle(CriarQuery());
        Assert.That(bytes, Is.Not.Null.And.Not.Empty);
    }

    // ── CA128: flag recibo_emitido_em gravada na 1ª emissão ──────────────────

    [Test]
    public async Task Handle_PrimeiraEmissao_GravaFlagRecibo()
    {
        var cobranca = CriarCobrancaComPagamento();
        var pagamento = cobranca.Pagamentos.First();

        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(cobranca);
        _cobrancaRepo.Setup(r => r.Salvar(cobranca)).Returns(Task.CompletedTask);
        _pdfService.DadosFixos = DadosPadrao();

        Assert.That(pagamento.ReciboEmitidoEm, Is.Null, "Deve iniciar null");

        await _sut.Handle(CriarQuery());

        Assert.That(pagamento.ReciboEmitidoEm, Is.Not.Null, "Deve ter timestamp após 1ª emissão");
        _cobrancaRepo.Verify(r => r.Salvar(cobranca), Times.AtLeastOnce);
    }

    // ── CA128: 2ª emissão não sobrescreve o timestamp ────────────────────────

    [Test]
    public async Task Handle_SegundaEmissao_NaoSobrescreveTimestamp()
    {
        var cobranca = CriarCobrancaComPagamento();
        var pagamento = cobranca.Pagamentos.First();

        _cobrancaRepo.Setup(r => r.ObterPorIdOuNulo(CobrancaId, EstabId)).ReturnsAsync(cobranca);
        _cobrancaRepo.Setup(r => r.Salvar(cobranca)).Returns(Task.CompletedTask);
        _pdfService.DadosFixos = DadosPadrao();

        await _sut.Handle(CriarQuery());
        var primeiro = pagamento.ReciboEmitidoEm;

        System.Threading.Thread.Sleep(10);
        await _sut.Handle(CriarQuery());

        Assert.That(pagamento.ReciboEmitidoEm, Is.EqualTo(primeiro),
            "Não deve sobrescrever na reemissão");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ReciboPagamentoRow DadosPadrao() => new(
        PagamentoId: PagamentoId,
        CobrancaId: CobrancaId,
        PacienteId: PacienteId,
        PacienteNome: "Maria da Silva",
        ValorPago: 200m,
        FormaPagamentoNome: "Dinheiro",
        Parcelas: 1,
        DataPagamento: DateOnly.FromDateTime(DateTime.Today),
        RegistradoPorNome: "Recepcionista Teste",
        CobrancaOrigem: "Consulta",
        CobrancaDescricao: "Consulta de rotina",
        EstabelecimentoNomeFantasia: "Clínica Teste",
        EstabelecimentoCnpj: null,
        EstabelecimentoTelefone: null,
        EstabelecimentoEndereco: null,
        EstabelecimentoFotoUrl: null);

    // ── Subclasse testável — sobrescreve a query Dapper ───────────────────────

    private sealed class TestableQuestPdfReciboPagamentoService : QuestPdfReciboPagamentoService
    {
        public ReciboPagamentoRow DadosFixos { get; set; }

        public TestableQuestPdfReciboPagamentoService(
            ICobrancaRepository cobrancaRepo,
            IPacienteAcessoLogService acessoLog)
            : base(
                new AppReadConnectionString("Host=localhost;"),
                new TestHttpClientFactory(),
                cobrancaRepo,
                acessoLog,
                NullLogger<QuestPdfReciboPagamentoService>.Instance)
        { }

        internal override Task<ReciboPagamentoRow> CarregarDadosAsync(long pagamentoId, long estabelecimentoId)
            => Task.FromResult(DadosFixos);
    }

    private sealed class TestHttpClientFactory : System.Net.Http.IHttpClientFactory
    {
        public System.Net.Http.HttpClient CreateClient(string name) => new();
    }
}
