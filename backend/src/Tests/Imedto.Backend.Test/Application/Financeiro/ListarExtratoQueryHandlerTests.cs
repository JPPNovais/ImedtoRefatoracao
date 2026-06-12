using Imedto.Backend.Application.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;
using System.Linq;

namespace Imedto.Backend.Test.Application.Financeiro;

/// <summary>
/// Testa validações e roteamento do ListarExtratoQueryHandler.
/// Cobre o modo normal (período) e o modo vencidos (SomenteVencidos=true, CA4/CA15/R4).
/// </summary>
[TestFixture]
public class ListarExtratoQueryHandlerTests
{
    private FakeExtrato _repo = null!;
    private ListarExtratoQueryHandler _sut = null!;

    private const long EstabelecimentoId = 7;

    [SetUp]
    public void SetUp()
    {
        _repo = new FakeExtrato();
        _sut = new ListarExtratoQueryHandler(_repo);
    }

    private static ListarExtratoQuery QueryNormal() => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        DataInicio = new DateOnly(2026, 1, 1),
        DataFim = new DateOnly(2026, 1, 31),
        Pagina = 1,
        TamanhoPagina = 20
    };

    // ─── Validações ────────────────────────────────────────────────────────────

    [Test]
    public void Handle_PaginaInvalida_LancaBusinessException()
    {
        var q = QueryNormal();
        q.Pagina = 0;
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(q));
    }

    [Test]
    public void Handle_TamanhoPaginaZero_LancaBusinessException()
    {
        var q = QueryNormal();
        q.TamanhoPagina = 0;
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(q));
    }

    [Test]
    public void Handle_TamanhoPaginaExcede100_LancaBusinessException()
    {
        var q = QueryNormal();
        q.TamanhoPagina = 101;
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(q));
    }

    // ─── Modo normal (CA15 — comportamento inalterado) ─────────────────────────

    [Test]
    public async Task Handle_ModoNormal_ChamaListarExtrato()
    {
        var q = QueryNormal();

        await _sut.Handle(q);

        Assert.That(_repo.ChamouListarExtrato, Is.True, "Deve chamar ListarExtrato no modo normal.");
        Assert.That(_repo.ChamouListarExtratoVencidos, Is.False);
    }

    [Test]
    public async Task Handle_ModoNormal_PassaEstabelecimentoIdCorreto()
    {
        var q = QueryNormal();
        q.EstabelecimentoId = 42;

        await _sut.Handle(q);

        Assert.That(_repo.UltimoEstabelecimentoId, Is.EqualTo(42));
    }

    // ─── Modo vencidos (CA4/R4) ────────────────────────────────────────────────

    [Test]
    public async Task Handle_ModoVencidos_ChamaListarExtratoVencidos()
    {
        var q = QueryNormal();
        q.SomenteVencidos = true;

        await _sut.Handle(q);

        Assert.That(_repo.ChamouListarExtratoVencidos, Is.True,
            "SomenteVencidos=true deve rotear para ListarExtratoVencidos.");
        Assert.That(_repo.ChamouListarExtrato, Is.False);
    }

    [Test]
    public async Task Handle_ModoVencidos_PassaEstabelecimentoIdCorreto_MultiTenant()
    {
        // Multi-tenant: estabelecimentoId deve ser preservado no modo vencidos (CA8).
        var q = QueryNormal();
        q.SomenteVencidos = true;
        q.EstabelecimentoId = 99;

        await _sut.Handle(q);

        Assert.That(_repo.UltimoEstabelecimentoId, Is.EqualTo(99));
    }

    [Test]
    public async Task Handle_ModoVencidos_RetornaResultadoDoRepositorio()
    {
        var itemVencido = new LancamentoExtratoDto
        {
            Id = 1, Descricao = "Consulta vencida", Valor = 150m,
            Tipo = "Receita", Status = "Pendente"
        };
        _repo.RetornoVencidos = new PaginaLancamentosExtratoDto
        {
            Itens = new[] { itemVencido },
            Total = 1, Pagina = 1, TamanhoPagina = 20
        };

        var q = QueryNormal();
        q.SomenteVencidos = true;

        var result = await _sut.Handle(q);

        Assert.That(result.Total, Is.EqualTo(1));
        Assert.That(result.Itens.First().Valor, Is.EqualTo(150m));
    }

    // ─── Fake ──────────────────────────────────────────────────────────────────

    private sealed class FakeExtrato : ConsolidacaoFinanceiraQueryRepository
    {
        public FakeExtrato() : base(new AppReadConnectionString("Host=fake")) { }

        public bool ChamouListarExtrato { get; private set; }
        public bool ChamouListarExtratoVencidos { get; private set; }
        public long UltimoEstabelecimentoId { get; private set; }

        public PaginaLancamentosExtratoDto RetornoNormal { get; set; } = new()
        {
            Itens = Array.Empty<LancamentoExtratoDto>(), Total = 0, Pagina = 1, TamanhoPagina = 20
        };

        public PaginaLancamentosExtratoDto RetornoVencidos { get; set; } = new()
        {
            Itens = Array.Empty<LancamentoExtratoDto>(), Total = 0, Pagina = 1, TamanhoPagina = 20
        };

        public override Task<PaginaLancamentosExtratoDto> ListarExtrato(
            long estabelecimentoId, DateOnly dataInicio, DateOnly dataFim,
            string? tipo, string? categoria, string? formaPagamento, string? origem,
            int pagina, int tamanhoPagina)
        {
            ChamouListarExtrato = true;
            UltimoEstabelecimentoId = estabelecimentoId;
            return Task.FromResult(RetornoNormal);
        }

        public override Task<PaginaLancamentosExtratoDto> ListarExtratoVencidos(
            long estabelecimentoId, string? tipo, string? categoria,
            string? formaPagamento, string? origem, int pagina, int tamanhoPagina)
        {
            ChamouListarExtratoVencidos = true;
            UltimoEstabelecimentoId = estabelecimentoId;
            return Task.FromResult(RetornoVencidos);
        }
    }
}
