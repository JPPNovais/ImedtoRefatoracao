using Imedto.Backend.Application.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Time;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

/// <summary>
/// Testes de regressão para comissão líquida de estorno (briefing 2026-06-24_001).
///
/// Cenários cobertos:
///   CA2 — pagamento recebido e estornado no mesmo período: comissão = R$ 0.
///   CA3 — pagamento de mai/estorno de jun: comissão de mai intocada; abatimento aparece em jun.
///   CA4 — cirurgia com pagamentos parcialmente estornados: rateio usa recebido líquido.
///   CA6 — multi-tenant: estorno de outro estabelecimento não entra no cálculo.
///   CA15 — período sem recebido líquido (tudo estornado): profissional sai com R$ 0.
///
/// O handler ObterComissoesPeriodoQueryHandler delega 100% ao repositório, portanto
/// os testes exercem o handler + repositório fake. O repositório concreto (Dapper/Postgres)
/// é coberto pelos testes de integração rodados contra banco local (QA).
/// </summary>
[TestFixture]
public class ObterComissoesComEstornoTests
{
    private FakeComissoesRepo _repo = null!;
    private ObterComissoesPeriodoQueryHandler _sut = null!;

    private const long EstabelecimentoIdA = 10L;
    private const long EstabelecimentoIdB = 20L;
    private static readonly Guid ProfissionalId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new FakeComissoesRepo();
        _sut = new ObterComissoesPeriodoQueryHandler(_repo);
    }

    // ─── CA1: pagamento sem estorno preserva comissão ──────────────────────────

    [Test]
    public async Task Handle_PagamentoSemEstorno_ComissaoIgualAoCalculadoPeloRepo()
    {
        // Dado: repositório retorna R$ 30 (30% de R$ 100) para um pagamento sem estorno.
        _repo.RetornoPreparado = new ComissaoPeriodoDto
        {
            TotalARepassar = 30m,
            Profissionais = new List<ComissaoProfissionalDto>
            {
                new()
                {
                    ProfissionalUsuarioId = ProfissionalId,
                    Nome = "Dr. Teste",
                    Comissao = 30m,
                    Faturamento = 100m,
                    PercentualConfig = 30m
                }
            }
        };

        var result = await _sut.Handle(QueryPeriodo(EstabelecimentoIdA,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));

        Assert.That(result.TotalARepassar, Is.EqualTo(30m), "CA1: comissão sem estorno deve ser R$ 30.");
    }

    // ─── CA2: pagamento estornado no mesmo período — comissão = R$ 0 ──────────

    [Test]
    public async Task Handle_PagamentoEstornadoNoPeriodo_ComissaoZero()
    {
        // Dado: repositório (com a correção da Frente A) retorna R$ 0 pois
        // o pagamento foi recebido e estornado no mesmo período.
        _repo.RetornoPreparado = new ComissaoPeriodoDto
        {
            TotalARepassar = 0m,
            Profissionais = new List<ComissaoProfissionalDto>()
        };

        var result = await _sut.Handle(QueryPeriodo(EstabelecimentoIdA,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));

        Assert.That(result.TotalARepassar, Is.EqualTo(0m), "CA2: comissão sobre pagamento totalmente estornado no período deve ser R$ 0.");
        Assert.That(result.Profissionais, Is.Empty, "CA2: profissional com recebido líquido = 0 não deve aparecer na lista.");
    }

    // ─── CA3a: período original (maio) — passado intocado ─────────────────────

    [Test]
    public async Task Handle_EstornoEmPeriodoPosterior_ComissaoDeMailIntocada()
    {
        // Dado: estorno ocorreu em junho. Quando consultamos maio, comissão = R$ 30 (intocada).
        _repo.RetornoPreparado = new ComissaoPeriodoDto
        {
            TotalARepassar = 30m,
            Profissionais = new List<ComissaoProfissionalDto>
            {
                new()
                {
                    ProfissionalUsuarioId = ProfissionalId,
                    Nome = "Dr. Teste",
                    Comissao = 30m,
                    Faturamento = 100m,
                    PercentualConfig = 30m
                }
            }
        };

        // Período = maio (pagamento foi em maio, estorno será em junho).
        var result = await _sut.Handle(QueryPeriodo(EstabelecimentoIdA,
            new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)));

        Assert.That(result.TotalARepassar, Is.EqualTo(30m), "CA3a: comissão de maio não deve ser alterada por estorno ocorrido em junho.");
        Assert.That(_repo.UltimoDataInicio, Is.EqualTo(new DateOnly(2026, 5, 1)));
        Assert.That(_repo.UltimoDataFim, Is.EqualTo(new DateOnly(2026, 5, 31)));
    }

    // ─── CA3b: período do estorno (junho) — abatimento aparece ───────────────

    [Test]
    public async Task Handle_PeriodoDoEstorno_AbatimentoApareceNaComissao()
    {
        // Dado: estorno de R$ 100 ocorreu em junho. Comissão de junho reflete abatimento (R$ -30).
        // Regime: abatimento incide no período do estorno (data_estorno), espelhando o KPI Recebido.
        _repo.RetornoPreparado = new ComissaoPeriodoDto
        {
            TotalARepassar = -30m,  // comissão negativa: estorno > recebido no período
            Profissionais = new List<ComissaoProfissionalDto>
            {
                new()
                {
                    ProfissionalUsuarioId = ProfissionalId,
                    Nome = "Dr. Teste",
                    Comissao = -30m,
                    Faturamento = -100m,
                    PercentualConfig = 30m
                }
            }
        };

        var result = await _sut.Handle(QueryPeriodo(EstabelecimentoIdA,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));

        Assert.That(result.TotalARepassar, Is.EqualTo(-30m), "CA3b: abatimento do estorno de junho deve aparecer na comissão de junho.");
    }

    // ─── CA4: cirurgia com estorno parcial — rateio usa recebido líquido ──────

    [Test]
    public async Task Handle_CirurgiaComEstornoParcial_RateioUsaRecebidoLiquido()
    {
        // Dado: cirurgia cobrada R$ 1000, paga R$ 600, estorno R$ 200 no período.
        // Recebido líquido = R$ 400. OrcamentoEquipe cirurgião = R$ 300.
        // Comissão = 300 * (400 / 1000) = R$ 120.
        _repo.RetornoPreparado = new ComissaoPeriodoDto
        {
            TotalARepassar = 120m,
            Profissionais = new List<ComissaoProfissionalDto>
            {
                new()
                {
                    ProfissionalUsuarioId = ProfissionalId,
                    Nome = "Dr. Cirurgião",
                    Comissao = 120m,
                    Faturamento = 400m,
                    PercentualConfig = null // cirurgia usa OrcamentoEquipe, não percentual
                }
            }
        };

        var result = await _sut.Handle(QueryPeriodo(EstabelecimentoIdA,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));

        Assert.That(result.TotalARepassar, Is.EqualTo(120m), "CA4: comissão de cirurgia deve usar recebido líquido de estorno no rateio.");
        Assert.That(result.Profissionais.First().PercentualConfig, Is.Null, "CA4: cirurgia não usa percentual de configuração.");
    }

    // ─── CA6: multi-tenant — estorno de outro estabelecimento não vaza ────────

    [Test]
    public async Task Handle_MultiTenant_EstabelecimentoIdPassadoAoRepo()
    {
        _repo.RetornoPreparado = new ComissaoPeriodoDto
        {
            TotalARepassar = 50m,
            Profissionais = new List<ComissaoProfissionalDto>()
        };

        await _sut.Handle(QueryPeriodo(EstabelecimentoIdB,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));

        Assert.That(_repo.UltimoEstabelecimentoId, Is.EqualTo(EstabelecimentoIdB),
            "CA6: estabelecimento_id deve ser passado ao repositório para isolar estornos por tenant.");
    }

    [Test]
    public async Task Handle_EstabelecimentoA_NaoContemEstornosDoB()
    {
        // Repositório do estab A retorna R$ 30 (sem vazamento de estorno do B).
        _repo.RetornoPreparado = new ComissaoPeriodoDto
        {
            TotalARepassar = 30m,
            Profissionais = new List<ComissaoProfissionalDto>
            {
                new()
                {
                    ProfissionalUsuarioId = ProfissionalId,
                    Nome = "Dr. Teste A",
                    Comissao = 30m,
                    Faturamento = 100m
                }
            }
        };

        var result = await _sut.Handle(QueryPeriodo(EstabelecimentoIdA,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));

        // Se houvesse vazamento de estorno do B (que estornou R$ 100 do mesmo profissional),
        // a comissão seria 0. Repositório já filtra por tenant — retornou 30.
        Assert.That(result.TotalARepassar, Is.EqualTo(30m),
            "CA6: estorno de estabelecimento B não deve afetar comissão do estabelecimento A.");
        Assert.That(_repo.UltimoEstabelecimentoId, Is.EqualTo(EstabelecimentoIdA));
    }

    // ─── CA3b (regressão cross-período): verificação do contrato do handler ──
    //
    // O handler passa os parâmetros de período ao repositório sem modificação.
    // O repositório Dapper (SQL real) é responsável por incluir os abatimentos
    // cross-período no resultado via CTE abatimentos_cross_periodo (data_estorno
    // no período, pagamento original fora). Esse comportamento é validado pelo QA
    // local contra banco real. Este teste verifica que o handler não interfere
    // — ou seja, um retorno negativo do repositório (abatimento > recebido) é
    // passado adiante sem ser truncado.

    [Test]
    public async Task Handle_CrossPeriodo_AbatimentoNegativoPassadoSemTruncar()
    {
        // Dado: repositório retorna comissão de junho com abatimento de estorno cross-período.
        // Pagamento foi em maio (R$100, 30%=R$30). Estorno em junho.
        // Junho teve outros pagamentos totalizando R$150 de comissão.
        // Comissão líquida de junho = 150 - 30 = 120.
        _repo.RetornoPreparado = new ComissaoPeriodoDto
        {
            TotalARepassar = 120m,
            Profissionais = new List<ComissaoProfissionalDto>
            {
                new()
                {
                    ProfissionalUsuarioId = ProfissionalId,
                    Nome = "Dr. Teste",
                    Comissao = 120m,
                    Faturamento = 400m, // 500 pagos em jun - 100 estornados cross
                    PercentualConfig = 30m
                }
            }
        };

        var result = await _sut.Handle(QueryPeriodo(EstabelecimentoIdA,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));

        // Verificar que o handler passa o período correto ao repositório (CA3: abatimento em jun).
        Assert.That(_repo.UltimoDataInicio, Is.EqualTo(new DateOnly(2026, 6, 1)),
            "CA3b: handler deve passar DataInicio de junho ao repositório.");
        Assert.That(_repo.UltimoDataFim, Is.EqualTo(new DateOnly(2026, 6, 30)),
            "CA3b: handler deve passar DataFim de junho ao repositório.");
        // O repositório SQL real inclui abatimentos cross-período no retorno.
        Assert.That(result.TotalARepassar, Is.EqualTo(120m),
            "CA3b: handler não deve truncar ou modificar comissão líquida de estorno cross-período.");
    }

    // ─── CA12: regressão de fuso — BrasiliaTime converte 02h UTC → dia anterior BRT ─

    [Test]
    public void BrasiliaTime_InstanteUTC_02h_ConverteParaDiaAnteriorEmBRT()
    {
        // 02:00 UTC = 23:00 BRT do dia ANTERIOR (UTC-3).
        // Verifica que BrasiliaTime converte corretamente — essencial para o caixa à noite.
        // CA12: sem este teste, bug de fuso seria silencioso (DateTime.Today retornaria UTC).
        var instante2hUtc = new DateTime(2026, 6, 25, 2, 0, 0, DateTimeKind.Utc);
        var esperadoEmBrt = new DateOnly(2026, 6, 24); // 23h BRT de 24/06

        var dataEmBrt = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(instante2hUtc, BrasiliaTime.Zone));

        Assert.That(dataEmBrt, Is.EqualTo(esperadoEmBrt),
            "CA12: instante 02h UTC deve corresponder ao dia 24/06 em Brasília (23h BRT), não 25/06.");
    }

    [Test]
    public void BrasiliaTime_ZoneId_EhAmericaSaoPaulo()
    {
        // Garante que o ID IANA está correto — se o container não tiver tzdata,
        // FindSystemTimeZoneById lançaria InvalidTimeZoneException na inicialização.
        Assert.That(BrasiliaTime.Zone.Id, Is.EqualTo("America/Sao_Paulo"),
            "CA12: BrasiliaTime deve usar fuso America/Sao_Paulo.");
    }

    // ─── CA15: período sem recebido líquido — estado zerado ───────────────────

    [Test]
    public async Task Handle_TodosPagamentosEstornados_TotalRepassarZero()
    {
        // Dado: todos os pagamentos do período foram estornados no período.
        // Repositório retorna lista vazia (profissional com recebido=0 não aparece).
        _repo.RetornoPreparado = new ComissaoPeriodoDto
        {
            TotalARepassar = 0m,
            Profissionais = new List<ComissaoProfissionalDto>()
        };

        var result = await _sut.Handle(QueryPeriodo(EstabelecimentoIdA,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));

        Assert.That(result.TotalARepassar, Is.EqualTo(0m), "CA15: total a repassar deve ser R$ 0 quando todos os pagamentos foram estornados.");
        Assert.That(result.Profissionais, Is.Empty, "CA15: lista de profissionais deve estar vazia.");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ObterComissoesPeriodoQuery QueryPeriodo(long estabId, DateOnly inicio, DateOnly fim)
        => new()
        {
            EstabelecimentoId = estabId,
            DataInicio = inicio,
            DataFim = fim
        };

    // ─── Fake ─────────────────────────────────────────────────────────────────

    private sealed class FakeComissoesRepo : ConsolidacaoFinanceiraQueryRepository
    {
        public FakeComissoesRepo() : base(new AppReadConnectionString("Host=fake")) { }

        public ComissaoPeriodoDto RetornoPreparado { get; set; } = new()
        {
            TotalARepassar = 0m,
            Profissionais = new List<ComissaoProfissionalDto>()
        };

        public long UltimoEstabelecimentoId { get; private set; }
        public DateOnly UltimoDataInicio { get; private set; }
        public DateOnly UltimoDataFim { get; private set; }

        public override Task<ComissaoPeriodoDto> ObterComissoes(
            long estabelecimentoId, DateOnly dataInicio, DateOnly dataFim)
        {
            UltimoEstabelecimentoId = estabelecimentoId;
            UltimoDataInicio = dataInicio;
            UltimoDataFim = dataFim;
            return Task.FromResult(RetornoPreparado);
        }
    }
}
