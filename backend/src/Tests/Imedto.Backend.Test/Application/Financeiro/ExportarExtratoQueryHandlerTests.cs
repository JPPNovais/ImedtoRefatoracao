using Imedto.Backend.Application.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

/// <summary>
/// Testa as validações e o fluxo da ExportarExtratoQueryHandler.
/// O repositório Dapper concreto é substituído por um fake para isolar
/// a lógica do handler sem dependência de banco.
/// </summary>
[TestFixture]
public class ExportarExtratoQueryHandlerTests
{
    private FakeConsolidacaoRepo _repo = null!;
    private ExportarExtratoQueryHandler _sut = null!;

    private const long EstabelecimentoId = 42;
    private readonly Guid _usuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new FakeConsolidacaoRepo();
        _sut = new ExportarExtratoQueryHandler(_repo);
    }

    private ExportarExtratoQuery QueryValida() => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        UsuarioId = _usuarioId,
        DataInicio = new DateOnly(2026, 1, 1),
        DataFim = new DateOnly(2026, 1, 31)
    };

    // ─── Validações de guard ────────────────────────────────────────────────────

    [Test]
    public void Handle_EstabelecimentoInvalido_LancaBusinessException()
    {
        var q = QueryValida();
        q.EstabelecimentoId = 0;
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(q));
    }

    [Test]
    public void Handle_DataInicioMaiorQueFim_LancaBusinessException()
    {
        var q = QueryValida();
        q.DataInicio = new DateOnly(2026, 2, 1);
        q.DataFim    = new DateOnly(2026, 1, 1);
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(q));
    }

    // ─── Caminho feliz ──────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_PeriodoComLinhas_RetornaItensETotal()
    {
        _repo.ItensPreparados = new List<LancamentoExtratoDto>
        {
            new() { Id = 1, Descricao = "Consulta", Valor = 200m, Tipo = "Receita", Status = "Pago" },
            new() { Id = 2, Descricao = "Despesa",  Valor = 50m,  Tipo = "Despesa", Status = "Pago" }
        };

        var result = await _sut.Handle(QueryValida());

        Assert.That(result.TotalLinhas, Is.EqualTo(2));
        Assert.That(result.Itens, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Handle_PeriodoSemLinhas_RetornaListaVaziaComCabecalho()
    {
        _repo.ItensPreparados = new List<LancamentoExtratoDto>();

        var result = await _sut.Handle(QueryValida());

        Assert.That(result.TotalLinhas, Is.EqualTo(0));
        Assert.That(result.Itens, Is.Empty);
    }

    [Test]
    public async Task Handle_PeriodoRetornadoNoResult_EqualAoQueryInput()
    {
        var q = QueryValida();
        _repo.ItensPreparados = new List<LancamentoExtratoDto>();

        var result = await _sut.Handle(q);

        Assert.That(result.DataInicio, Is.EqualTo(q.DataInicio));
        Assert.That(result.DataFim,    Is.EqualTo(q.DataFim));
    }

    // ─── Audit best-effort ─────────────────────────────────────────────────────

    [Test]
    public async Task Handle_AuditFalhaGraceful_NaoLancaExcecao()
    {
        _repo.ItensPreparados = new List<LancamentoExtratoDto>
        {
            new() { Id = 1, Descricao = "Consulta", Valor = 100m, Tipo = "Receita", Status = "Pago" }
        };
        _repo.AuditDevefalhar = true;

        // Mesmo com audit falhando, o resultado deve ser retornado normalmente.
        var result = await _sut.Handle(QueryValida());
        Assert.That(result.TotalLinhas, Is.EqualTo(1));
    }

    // ─── Multi-tenant ──────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_EstabelecimentoIdPassadoParaRepositorio_CorretoMultiTenant()
    {
        _repo.ItensPreparados = new List<LancamentoExtratoDto>();
        var q = QueryValida();
        q.EstabelecimentoId = 99;

        await _sut.Handle(q);

        Assert.That(_repo.UltimoEstabelecimentoId, Is.EqualTo(99));
    }

    // ─── Fake (test double) do repositório ─────────────────────────────────────

    private sealed class FakeConsolidacaoRepo : ConsolidacaoFinanceiraQueryRepository
    {
        // Construtor dummy — não conecta a banco.
        public FakeConsolidacaoRepo() : base(new AppReadConnectionString("Host=fake")) { }

        public List<LancamentoExtratoDto> ItensPreparados { get; set; } = new();
        public bool AuditDevefalhar { get; set; }
        public long UltimoEstabelecimentoId { get; private set; }

        public override Task<List<LancamentoExtratoDto>> ExportarExtrato(
            long estabelecimentoId, DateOnly dataInicio, DateOnly dataFim,
            string? tipo, string? categoria, string? formaPagamento, string? origem)
        {
            UltimoEstabelecimentoId = estabelecimentoId;
            return Task.FromResult(ItensPreparados);
        }

        public override Task GravarExportAuditAsync(
            Guid usuarioId, long estabelecimentoId,
            DateOnly dataInicio, DateOnly dataFim, int totalLinhas)
        {
            if (AuditDevefalhar) throw new Exception("Falha simulada de audit.");
            return Task.CompletedTask;
        }
    }
}
