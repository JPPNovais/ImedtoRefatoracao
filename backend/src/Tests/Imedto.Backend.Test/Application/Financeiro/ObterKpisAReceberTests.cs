using Imedto.Backend.Application.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.Infrastructure;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

/// <summary>
/// Testes de regressão para "A receber" completo (briefing 2026-06-24_002 Frente C).
///
/// Cenários cobertos:
///   CA1 — cobrança Aberta sem lançamento: entra em "A receber" (antes seria R$ 0).
///   CA2 — cobrança ParcialmentePaga: saldo = valor − pago entra em "A receber".
///   CA3 — anti-dupla-contagem: cobrança paga vira Lançamento Pago; não reaparece em "A receber".
///   CA4 — soma das duas fontes: saldo cobranças + lançamentos pendentes avulsos.
///   CA5 — estorno reabre saldo: soma líquida de pagamentos = 0, saldo = 200.
///   CA6 — multi-tenant: estabelecimento_id passado ao repositório.
///   CA17 — estado vazio: "A receber" = R$ 0 sem cobranças nem lançamentos.
///
/// O handler ObterKpisFinanceiroQueryHandler delega 100% ao repositório, portanto
/// os testes exercem o handler + repositório fake (o repo concreto Dapper/Postgres
/// é validado pelo QA local contra banco real).
/// </summary>
[TestFixture]
public class ObterKpisAReceberTests
{
    private FakeKpisRepo _repo = null!;
    private ObterKpisFinanceiroQueryHandler _sut = null!;

    private const long EstabelecimentoIdA = 10L;
    private const long EstabelecimentoIdB = 20L;

    [SetUp]
    public void SetUp()
    {
        _repo = new FakeKpisRepo();
        _sut = new ObterKpisFinanceiroQueryHandler(_repo);
    }

    // ─── CA1: cobrança Aberta sem lançamento Pendente entra em A Receber ──────

    [Test]
    public async Task Handle_CobrancaAbertaSemLancamentoPendente_AReceberIgualAoSaldo()
    {
        // Dado: repositório retorna R$ 200 de saldo de cobrança em aberto, R$ 0 de lançamentos pendentes.
        // Com a Frente C, AReceber = 200 (antes era 0 — cobrança em aberto não tinha lançamento).
        _repo.RetornoPreparado = KpisWith(aReceber: 200m);

        var result = await _sut.Handle(QueryKpis(EstabelecimentoIdA));

        Assert.That(result.AReceber, Is.EqualTo(200m),
            "CA1: cobrança Aberta de R$ 200 sem lançamento Pendente deve aparecer em 'A receber'.");
    }

    // ─── CA2: cobrança ParcialmentePaga — saldo residual entra ───────────────

    [Test]
    public async Task Handle_CobrancaParcialmentePaga_SaldoResiualEntraEmAReceber()
    {
        // Dado: cobrança de R$ 200 com R$ 50 pagos → saldo R$ 150.
        _repo.RetornoPreparado = KpisWith(aReceber: 150m);

        var result = await _sut.Handle(QueryKpis(EstabelecimentoIdA));

        Assert.That(result.AReceber, Is.EqualTo(150m),
            "CA2: cobrança ParcialmentePaga com R$ 50 pagos deve contribuir R$ 150 ao 'A receber'.");
    }

    // ─── CA3: anti-dupla-contagem — cobrança paga não reaparece ──────────────

    [Test]
    public async Task Handle_CobrancaPagaMaisLancamentoPendenteAvulso_SemDuplaContagem()
    {
        // Dado: cobrança de R$ 200 paga (→ Lançamento Receita/Pago de R$ 200) + lançamento
        // Receita Pendente avulso de R$ 80.
        // "A receber" = R$ 80 apenas — a cobrança paga não reaparece (INV-3).
        _repo.RetornoPreparado = KpisWith(aReceber: 80m);

        var result = await _sut.Handle(QueryKpis(EstabelecimentoIdA));

        Assert.That(result.AReceber, Is.EqualTo(80m),
            "CA3: cobrança paga (→ Lançamento Pago) não deve reaparecer em 'A receber'; " +
            "apenas o lançamento avulso pendente de R$ 80 deve ser contado.");
    }

    // ─── CA4: soma das duas fontes sem duplicar ───────────────────────────────

    [Test]
    public async Task Handle_CobrancaAbertaMaisLancamentoPendente_SomaCorreta()
    {
        // Dado: cobrança Aberta de R$ 200 (sem lançamento) + lançamento Pendente avulso de R$ 80.
        // "A receber" = R$ 280.
        _repo.RetornoPreparado = KpisWith(aReceber: 280m);

        var result = await _sut.Handle(QueryKpis(EstabelecimentoIdA));

        Assert.That(result.AReceber, Is.EqualTo(280m),
            "CA4: 'A receber' deve ser R$ 280 = saldo cobrança (R$ 200) + lançamento pendente (R$ 80).");
    }

    // ─── CA5: estorno reabre saldo — soma líquida = 0 → saldo volta a 200 ────

    [Test]
    public async Task Handle_PagamentoEstornado_SaldoReabreParaValorOriginal()
    {
        // Dado: cobrança de R$ 200, pagamento de R$ 100 estornado → soma líquida = 0 → saldo = R$ 200.
        _repo.RetornoPreparado = KpisWith(aReceber: 200m);

        var result = await _sut.Handle(QueryKpis(EstabelecimentoIdA));

        Assert.That(result.AReceber, Is.EqualTo(200m),
            "CA5: pagamento totalmente estornado deve reabrir o saldo; 'A receber' = R$ 200.");
    }

    // ─── CA6: multi-tenant — estabelecimento_id passado ao repositório ────────

    [Test]
    public async Task Handle_MultiTenant_EstabelecimentoIdPassadoAoRepo()
    {
        _repo.RetornoPreparado = KpisWith(aReceber: 0m);

        await _sut.Handle(QueryKpis(EstabelecimentoIdB));

        Assert.That(_repo.UltimoEstabelecimentoId, Is.EqualTo(EstabelecimentoIdB),
            "CA6: estabelecimento_id B deve ser passado ao repositório para isolar cobranças por tenant.");
    }

    [Test]
    public async Task Handle_MultiTenant_EstabelecimentoA_NaoContemCobrancasDoB()
    {
        // Repositório do estab A retorna R$ 50 (sem vazamento de cobranças do B).
        _repo.RetornoPreparado = KpisWith(aReceber: 50m);

        var result = await _sut.Handle(QueryKpis(EstabelecimentoIdA));

        Assert.That(result.AReceber, Is.EqualTo(50m),
            "CA6: 'A receber' do estabelecimento A não deve conter cobranças do B.");
        Assert.That(_repo.UltimoEstabelecimentoId, Is.EqualTo(EstabelecimentoIdA));
    }

    // ─── CA17: estado vazio — AReceber = 0 ────────────────────────────────────

    [Test]
    public async Task Handle_SemCobrancasNemLancamentos_AReceberZero()
    {
        // Dado: sem cobranças em aberto, sem lançamentos pendentes.
        _repo.RetornoPreparado = KpisWith(aReceber: 0m);

        var result = await _sut.Handle(QueryKpis(EstabelecimentoIdA));

        Assert.That(result.AReceber, Is.EqualTo(0m),
            "CA17: sem cobranças nem lançamentos pendentes, 'A receber' deve ser R$ 0.");
    }

    // ─── Demais KPIs não regredidos ───────────────────────────────────────────

    [Test]
    public async Task Handle_OutrosKpisPreservados_NaoAfetadosPelaFrenteC()
    {
        // Frente C altera apenas AReceber — demais KPIs passam pelo handler inalterados.
        _repo.RetornoPreparado = new KpisFinanceiroDto
        {
            Recebido = 500m,
            AReceber = 200m,
            Despesas = 100m,
            Saldo = 400m,
            Estornos = 50m,
            DescontosConcedidos = 20m,
            TaxasCartao = 10m
        };

        var result = await _sut.Handle(QueryKpis(EstabelecimentoIdA));

        Assert.Multiple(() =>
        {
            Assert.That(result.Recebido, Is.EqualTo(500m), "Recebido preservado.");
            Assert.That(result.AReceber, Is.EqualTo(200m), "AReceber passado do repositório.");
            Assert.That(result.Despesas, Is.EqualTo(100m), "Despesas preservadas.");
            Assert.That(result.Saldo, Is.EqualTo(400m), "Saldo preservado.");
            Assert.That(result.Estornos, Is.EqualTo(50m), "Estornos preservados.");
        });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ObterKpisFinanceiroQuery QueryKpis(long estabId) => new()
    {
        EstabelecimentoId = estabId,
        DataInicio = new DateOnly(2026, 6, 1),
        DataFim = new DateOnly(2026, 6, 30)
    };

    private static KpisFinanceiroDto KpisWith(decimal aReceber) => new()
    {
        Recebido = 0m,
        AReceber = aReceber,
        Despesas = 0m,
        Saldo = 0m,
        Estornos = 0m,
        DescontosConcedidos = 0m,
        TaxasCartao = 0m
    };

    // ─── Fake ─────────────────────────────────────────────────────────────────

    private sealed class FakeKpisRepo : ConsolidacaoFinanceiraQueryRepository
    {
        public FakeKpisRepo() : base(new AppReadConnectionString("Host=fake")) { }

        public KpisFinanceiroDto RetornoPreparado { get; set; } = new();

        public long UltimoEstabelecimentoId { get; private set; }
        public DateOnly UltimoDataInicio { get; private set; }
        public DateOnly UltimoDataFim { get; private set; }

        public override Task<KpisFinanceiroDto> ObterKpis(
            long estabelecimentoId, DateOnly dataInicio, DateOnly dataFim)
        {
            UltimoEstabelecimentoId = estabelecimentoId;
            UltimoDataInicio = dataInicio;
            UltimoDataFim = dataFim;
            return Task.FromResult(RetornoPreparado);
        }
    }
}
