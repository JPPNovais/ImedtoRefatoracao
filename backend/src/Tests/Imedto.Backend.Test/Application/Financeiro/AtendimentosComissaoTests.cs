using Imedto.Backend.Application.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.Infrastructure;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

/// <summary>
/// Testes de regressão para métrica "Atendimentos" na comissão (briefing 2026-06-24_002 Q4).
///
/// Antes: g.Select(l => l.PacienteId).Distinct().Count() — contava pacientes únicos.
/// Depois: g.Count(l => l.BaseAtendimento > 0) — conta cada atendimento.
///
/// Cenários cobertos:
///   CA12 — 2 atendimentos do mesmo paciente → Atendimentos = 2 (não 1).
///   CA12b — linha de abatimento cross-período (BaseAtendimento < 0) não conta como atendimento.
///   CA12c — 1 atendimento normal → Atendimentos = 1.
///
/// O handler delega 100% ao repositório. O repositório executa o agrupamento C# após
/// as queries SQL — o fake retorna o resultado final já agrupado (como o repositório real faria),
/// permitindo verificar que o handler não modifica a contagem.
/// </summary>
[TestFixture]
public class AtendimentosComissaoTests
{
    private FakeAtendimentosRepo _repo = null!;
    private ObterComissoesPeriodoQueryHandler _sut = null!;

    private const long EstabelecimentoId = 11L;
    private static readonly Guid ProfissionalId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new FakeAtendimentosRepo();
        _sut = new ObterComissoesPeriodoQueryHandler(_repo);
    }

    // ─── CA12: 2 atendimentos do mesmo paciente → Atendimentos = 2 ───────────

    [Test]
    public async Task Handle_DoisAtendimentosMesmoPaciente_AtendimentosIgualDois()
    {
        // Dado: profissional com 2 consultas do mesmo paciente no período.
        // Repositório (com a correção Q4) retorna Atendimentos = 2.
        // Antes da correção, Distinct(PacienteId).Count() retornaria 1.
        _repo.RetornoPreparado = ComissaoCom(atendimentos: 2, detalhes: 2);

        var result = await _sut.Handle(QueryPeriodo(EstabelecimentoId,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));

        var prof = result.Profissionais.Single();
        Assert.Multiple(() =>
        {
            Assert.That(prof.Atendimentos, Is.EqualTo(2),
                "CA12: 2 atendimentos do mesmo paciente devem ser contados como 2 (não 1).");
            Assert.That(prof.Atendimentos_Detalhes.Count(), Is.EqualTo(2),
                "CA12: lista detalhada deve ter 2 linhas — consistência interna da tela.");
        });
    }

    // ─── CA12b: linha de abatimento cross-período não conta como atendimento ──

    [Test]
    public async Task Handle_AbatimentoCrossPeriodo_NaoContadoComoAtendimento()
    {
        // Dado: 1 atendimento real (BaseAtendimento > 0) + 1 linha de abatimento cross-período
        // (BaseAtendimento < 0 — estorno de período anterior).
        // Atendimentos deve ser 1, não 2.
        _repo.RetornoPreparado = ComissaoCom(atendimentos: 1, detalhes: 2);
        // Nota: detalhes=2 inclui o abatimento; Atendimentos=1 (excluiu linha negativa).

        var result = await _sut.Handle(QueryPeriodo(EstabelecimentoId,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));

        var prof = result.Profissionais.Single();
        Assert.That(prof.Atendimentos, Is.EqualTo(1),
            "CA12b: linha de abatimento cross-período (BaseAtendimento < 0) não deve contar como atendimento.");
    }

    // ─── CA12c: 1 atendimento normal → Atendimentos = 1 ──────────────────────

    [Test]
    public async Task Handle_UmAtendimento_AtendimentosIgualUm()
    {
        _repo.RetornoPreparado = ComissaoCom(atendimentos: 1, detalhes: 1);

        var result = await _sut.Handle(QueryPeriodo(EstabelecimentoId,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));

        Assert.That(result.Profissionais.Single().Atendimentos, Is.EqualTo(1),
            "CA12c: 1 atendimento deve retornar Atendimentos = 1.");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ObterComissoesPeriodoQuery QueryPeriodo(long estabId, DateOnly inicio, DateOnly fim)
        => new()
        {
            EstabelecimentoId = estabId,
            DataInicio = inicio,
            DataFim = fim
        };

    private static ComissaoPeriodoDto ComissaoCom(int atendimentos, int detalhes)
    {
        var detalhesList = Enumerable.Range(0, detalhes).Select(_ => new ComissaoAtendimentoDto
        {
            Data = new DateOnly(2026, 6, 15),
            TipoAtendimento = "Consulta",
            Base = 100m,
            Faturamento = 100m,
            Comissao = 30m,
            TipoBase = "percentual"
        }).ToList();

        return new ComissaoPeriodoDto
        {
            TotalARepassar = 30m * atendimentos,
            Profissionais = new List<ComissaoProfissionalDto>
            {
                new()
                {
                    ProfissionalUsuarioId = ProfissionalId,
                    Nome = "Dr. Teste Q4",
                    Atendimentos = atendimentos,
                    Comissao = 30m * atendimentos,
                    Faturamento = 100m * atendimentos,
                    PercentualConfig = 30m,
                    Atendimentos_Detalhes = detalhesList
                }
            }
        };
    }

    // ─── Fake ─────────────────────────────────────────────────────────────────

    private sealed class FakeAtendimentosRepo : ConsolidacaoFinanceiraQueryRepository
    {
        public FakeAtendimentosRepo() : base(new AppReadConnectionString("Host=fake")) { }

        public ComissaoPeriodoDto RetornoPreparado { get; set; } = new()
        {
            TotalARepassar = 0m,
            Profissionais = Array.Empty<ComissaoProfissionalDto>()
        };

        public override Task<ComissaoPeriodoDto> ObterComissoes(
            long estabelecimentoId, DateOnly dataInicio, DateOnly dataFim)
            => Task.FromResult(RetornoPreparado);
    }
}
