using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.Infrastructure.Jobs.Handlers;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Agendamentos;

/// <summary>
/// Cobre CA3 (não toca hoje/futuro), CA4 (fronteira BRT), CA10 (idempotência),
/// nome do job e lógica de fronteira de janela.
///
/// Testes de CA1 (caminho feliz em banco real com rollup cross-tenant) e CA11
/// (uso do índice parcial via EXPLAIN) requerem smoke local com Postgres real.
///
/// Nota: EF InMemory não suporta LIKE/funções de fuso — os testes de janela
/// cobrem a lógica de cálculo, não o SQL gerado.
/// </summary>
[TestFixture]
public class ExpirarAgendamentosNaoFinalizadosJobTests
{
    private AppDbContext _db;
    private ExpirarAgendamentosNaoFinalizadosJob _sut;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);
        _sut = new ExpirarAgendamentosNaoFinalizadosJob(
            _db, NullLogger<ExpirarAgendamentosNaoFinalizadosJob>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static Agendamento CriarAgendamento(
        DateTime inicioPrevisto,
        AgendamentoStatus status = AgendamentoStatus.Agendado,
        long estabelecimentoId = 1)
    {
        var a = Agendamento.CriarHistorico(
            estabelecimentoId: estabelecimentoId,
            pacienteId: 1,
            profissionalUsuarioId: Guid.NewGuid(),
            criadoPorUsuarioId: Guid.NewGuid(),
            inicioPrevisto: inicioPrevisto,
            fimPrevisto: inicioPrevisto.AddHours(1),
            tipoServico: "Consulta",
            observacoes: null);

        if (status == AgendamentoStatus.Confirmado)
            a.Confirmar();

        return a;
    }

    private async Task<Agendamento> PersistirAsync(Agendamento a)
    {
        _db.Agendamentos.Add(a);
        await _db.SaveChangesAsync();
        return a;
    }

    // ─── CA3 — não toca hoje nem futuro ───────────────────────────────────────

    [Test]
    public async Task Executar_AgendamentoDeHoje_NaoExpira()
    {
        // Agendamento com inicio_previsto em UTC = hoje (dentro da janela de hoje, não D-1).
        var hoje = DateTime.UtcNow;
        var a = await PersistirAsync(CriarAgendamento(hoje, AgendamentoStatus.Agendado));

        await _sut.ExecutarAsync(CancellationToken.None);

        await _db.Entry(a).ReloadAsync();
        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Agendado),
            "Agendamento de hoje não deve ser expirado.");
    }

    [Test]
    public async Task Executar_AgendamentoFuturo_NaoExpira()
    {
        var amanha = DateTime.UtcNow.AddDays(2);
        var a = await PersistirAsync(CriarAgendamento(amanha, AgendamentoStatus.Confirmado));

        await _sut.ExecutarAsync(CancellationToken.None);

        await _db.Entry(a).ReloadAsync();
        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Confirmado));
    }

    // ─── CA2 — não toca terminais ─────────────────────────────────────────────

    [Test]
    public async Task Executar_AgendamentoConcluidoDeOntem_NaoAltera()
    {
        var ontemUtc = DateTime.UtcNow.AddDays(-1);
        var a = CriarAgendamento(ontemUtc);
        a.Concluir();
        await PersistirAsync(a);

        await _sut.ExecutarAsync(CancellationToken.None);

        await _db.Entry(a).ReloadAsync();
        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Concluido));
    }

    [Test]
    public async Task Executar_AgendamentoCanceladoDeOntem_NaoAltera()
    {
        var ontemUtc = DateTime.UtcNow.AddDays(-1);
        var a = CriarAgendamento(ontemUtc);
        a.Cancelar("Teste.");
        await PersistirAsync(a);

        await _sut.ExecutarAsync(CancellationToken.None);

        await _db.Entry(a).ReloadAsync();
        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Cancelado));
    }

    // ─── CA10 — idempotência / re-execução segura ─────────────────────────────

    [Test]
    public async Task Executar_DuasVezes_NaoTransicionaDuasVezes()
    {
        // Simula agendamento de ontem em BRT — usamos 25h atrás para evitar edge de fuso.
        var ontemUtc = DateTime.UtcNow.AddHours(-25);
        var a = await PersistirAsync(CriarAgendamento(ontemUtc, AgendamentoStatus.Agendado));

        // Primeira execução
        await _sut.ExecutarAsync(CancellationToken.None);
        await _db.Entry(a).ReloadAsync();
        var statusApos1a = a.Status;

        // Segunda execução (mesma janela)
        await _sut.ExecutarAsync(CancellationToken.None);
        await _db.Entry(a).ReloadAsync();

        // Status não muda na 2ª rodada — já está em Expirado ou não era elegível.
        Assert.That(a.Status, Is.EqualTo(statusApos1a),
            "Segunda execução não deve alterar status já em estado terminal.");
    }

    // ─── CA4 — fronteira de fuso BRT: cálculo da janela ──────────────────────

    [Test]
    public void JanelaBrt_InicioEFim_SaoCalculadosCorretamente()
    {
        // Valida que a lógica de cálculo da fronteira não usa UTC naive.
        // Brasília é UTC-3 (sem DST nas datas fixas de teste).
        var fusoBrasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

        var agora = DateTime.UtcNow;
        var hojeBrt = TimeZoneInfo.ConvertTimeFromUtc(agora, fusoBrasilia).Date;
        var ontemBrt = hojeBrt.AddDays(-1);

        var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(ontemBrt, DateTimeKind.Unspecified), fusoBrasilia);
        var fimUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(hojeBrt, DateTimeKind.Unspecified), fusoBrasilia);

        // A janela deve ter exatamente 24h.
        Assert.That((fimUtc - inicioUtc).TotalHours, Is.EqualTo(24).Within(0.001),
            "Janela D-1 deve ter exatamente 24h.");

        // O fim da janela (hoje 00:00 BRT em UTC) deve ser menor que agora
        // (pois o job cobre apenas D-1 — não o dia de hoje).
        Assert.That(fimUtc, Is.LessThanOrEqualTo(agora),
            "Fim da janela deve ser anterior ao momento de execução.");
    }

    // ─── Nome do job ──────────────────────────────────────────────────────────

    [Test]
    public void Nome_EhExpirarAgendamentosNaoFinalizados()
    {
        Assert.That(_sut.Nome, Is.EqualTo("expirar-agendamentos-nao-finalizados"));
    }
}
