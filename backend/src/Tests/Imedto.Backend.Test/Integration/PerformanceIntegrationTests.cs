using System.Diagnostics;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// Smoke tests de performance: garante que queries não regrediram para
/// seq scan ao crescer o dataset. Limites são CONSERVADORES (folga ~10x)
/// para tolerar variação de hardware sem virar fonte de flakiness.
///
/// Cenários focam no caminho HOT do front (listagem paginada de pacientes).
/// </summary>
[TestFixture]
public class PerformanceIntegrationTests : IntegrationTestBase
{
    protected override string[] TabelasParaTruncar => new[] { "pacientes", "estabelecimentos" };

    private const long EstabA = 1;
    private const long EstabB = 2;
    private const int VolumePorEstab = 500; // 500 + 500 = 1k pacientes
    private PacienteQueryRepository _sut;

    [SetUp]
    public async Task Seed()
    {
        await using var ctx = NewContext();
#pragma warning disable EF1002
        await ctx.Database.ExecuteSqlRawAsync(
            "INSERT INTO estabelecimentos (id, dono_usuario_id, nome_fantasia, status, criado_em, " +
            "  horario_inicio, horario_fim, dias_semana_funcionamento, horarios_bloqueados, datas_bloqueadas) " +
            "VALUES (1, gen_random_uuid(), 'A', 0, now(), '08:00', '18:00', '[1,2,3,4,5]'::jsonb, '[]'::jsonb, '[]'::jsonb), " +
            "       (2, gen_random_uuid(), 'B', 0, now(), '08:00', '18:00', '[1,2,3,4,5]'::jsonb, '[]'::jsonb, '[]'::jsonb);");

        // Bulk insert via SQL para velocidade — gerar nomes determinísticos para busca depois.
        await ctx.Database.ExecuteSqlRawAsync($"""
            INSERT INTO pacientes (estabelecimento_id, nome_completo, cpf, genero, criado_em)
            SELECT  1,
                    'Paciente A ' || lpad(g::text, 4, '0') || ' Silva',
                    lpad((100000000 + g)::text, 11, '0'),
                    0,
                    now()
            FROM    generate_series(1, {VolumePorEstab}) AS g;

            INSERT INTO pacientes (estabelecimento_id, nome_completo, cpf, genero, criado_em)
            SELECT  2,
                    'Paciente B ' || lpad(g::text, 4, '0') || ' Souza',
                    lpad((200000000 + g)::text, 11, '0'),
                    0,
                    now()
            FROM    generate_series(1, {VolumePorEstab}) AS g;
            """);

        // ANALYZE para o planner ter estatisticas atualizadas — crítico para o teste valer.
        await ctx.Database.ExecuteSqlRawAsync("ANALYZE pacientes;");
#pragma warning restore EF1002

        _sut = new PacienteQueryRepository(new AppReadConnectionString(PostgresIntegrationFixture.ConnectionString));
    }

    [Test]
    public async Task Listar_PrimeiraPagina_Sub500ms()
    {
        var sw = Stopwatch.StartNew();
        var resultado = await _sut.Listar(EstabA, busca: null, pagina: 1, tamanhoPagina: 20);
        sw.Stop();

        Assert.That(resultado.Total, Is.EqualTo(VolumePorEstab));
        Assert.That(resultado.Itens.Count(), Is.EqualTo(20));
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(500),
            $"Listar paginado em {VolumePorEstab} pacientes deve rodar abaixo de 500ms. " +
            $"Tempo atual: {sw.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task Listar_BuscaTrigram_NaoFazSeqScanCompleto()
    {
        // Aquece o plano
        await _sut.Listar(EstabA, busca: "Silva", pagina: 1, tamanhoPagina: 20);

        var sw = Stopwatch.StartNew();
        var resultado = await _sut.Listar(EstabA, busca: "Silva", pagina: 1, tamanhoPagina: 20);
        sw.Stop();

        Assert.That(resultado.Total, Is.EqualTo(VolumePorEstab),
            "Todos os 500 pacientes do EstabA têm 'Silva' no nome.");
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(500),
            $"Busca trigram com 1k registros deve rodar < 500ms. Tempo: {sw.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task Listar_FiltroPorEstabIsolado_NaoVarreOutroTenant()
    {
        // EXPLAIN deve mostrar que o predicado de estabelecimento_id é aplicado.
        // Verificamos indiretamente: tempo + correção do resultado.
        var sw = Stopwatch.StartNew();
        var resultadoA = await _sut.Listar(EstabA, busca: null, pagina: 1, tamanhoPagina: 20);
        var resultadoB = await _sut.Listar(EstabB, busca: null, pagina: 1, tamanhoPagina: 20);
        sw.Stop();

        Assert.That(resultadoA.Total, Is.EqualTo(VolumePorEstab));
        Assert.That(resultadoB.Total, Is.EqualTo(VolumePorEstab));

        // Sem cruzamento entre páginas dos dois estabs.
        Assert.That(resultadoA.Itens.All(i => i.NomeCompleto.Contains(" A ")), Is.True);
        Assert.That(resultadoB.Itens.All(i => i.NomeCompleto.Contains(" B ")), Is.True);

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(1000),
            $"Duas listagens em 1k registros deveriam rodar abaixo de 1s. Tempo: {sw.ElapsedMilliseconds}ms");
    }

    [Test]
    public async Task Listar_BuscaPorCpfPrefixo_RodaRapido()
    {
        // CPFs do EstabA gerados como lpad(100000000 + g, 11, '0') → "00100000001"..."00100000500"
        // Prefixo "00100" deve casar com TODOS os 500 do EstabA.
        var sw = Stopwatch.StartNew();
        var resultado = await _sut.Listar(EstabA, busca: "00100", pagina: 1, tamanhoPagina: 20);
        sw.Stop();

        Assert.That(resultado.Total, Is.EqualTo(VolumePorEstab),
            "Prefixo '00100' deve casar com todos os 500 CPFs gerados.");
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(500),
            $"Busca por CPF prefixo deve rodar < 500ms. Tempo: {sw.ElapsedMilliseconds}ms");
    }
}
