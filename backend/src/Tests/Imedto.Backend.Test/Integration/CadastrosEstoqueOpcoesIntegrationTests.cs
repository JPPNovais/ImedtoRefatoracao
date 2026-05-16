using Dapper;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Database.Repositories.Cadastros;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NUnit.Framework;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// Garante que os 4 endpoints de "opções" (dropdowns) do CadastrosEstoqueQueryRepository:
/// - filtram por estabelecimento (não vazam dados de outro tenant);
/// - retornam só registros com ativo=true;
/// - ordenam por nome (lowercase) e retornam apenas { Id, Nome }.
///
/// Seed via SQL direto pra evitar a regra de CNPJ válido do aggregate Fornecedor —
/// aqui o foco é a query Dapper, não o domínio.
/// </summary>
[TestFixture]
public class CadastrosEstoqueOpcoesIntegrationTests : IntegrationTestBase
{
    protected override string[] TabelasParaTruncar => new[]
    {
        "categorias_estoque",
        "fabricantes_estoque",
        "fornecedores_estoque",
        "locais_estoque",
        "estabelecimentos"
    };

    private const long EstabA = 100;
    private const long EstabB = 200;
    private CadastrosEstoqueQueryRepository _sut;

    [SetUp]
    public async Task Seed()
    {
        await using var ctx = NewContext();
#pragma warning disable EF1002
        await ctx.Database.ExecuteSqlRawAsync(
            "INSERT INTO estabelecimentos (id, dono_usuario_id, nome_fantasia, status, criado_em, " +
            "  horario_inicio, horario_fim, dias_semana_funcionamento, horarios_bloqueados, datas_bloqueadas) " +
            "VALUES (100, gen_random_uuid(), 'A', 0, now(), '08:00', '18:00', '[1,2,3,4,5]'::jsonb, '[]'::jsonb, '[]'::jsonb), " +
            "       (200, gen_random_uuid(), 'B', 0, now(), '08:00', '18:00', '[1,2,3,4,5]'::jsonb, '[]'::jsonb, '[]'::jsonb);");

        // Categorias: 2 ativas em A (uma "Bandagens", outra "Anestésicos"), 1 inativa em A,
        // 1 ativa em B com nome que se ordenado junto causaria vazamento.
        await ctx.Database.ExecuteSqlRawAsync(
            "INSERT INTO categorias_estoque (estabelecimento_id, nome, cor, icone, ativo, criado_em) VALUES " +
            "(100, 'Bandagens',  'hsl(0 70% 50%)', 'fa-bandage', TRUE,  now()), " +
            "(100, 'Anestésicos','hsl(0 70% 50%)', 'fa-syringe', TRUE,  now()), " +
            "(100, 'Velha',      'hsl(0 70% 50%)', 'fa-box',     FALSE, now()), " +
            "(200, 'B-categoria','hsl(0 70% 50%)', 'fa-pills',   TRUE,  now());");

        await ctx.Database.ExecuteSqlRawAsync(
            "INSERT INTO fabricantes_estoque (estabelecimento_id, nome, pais, ativo, criado_em) VALUES " +
            "(100, 'Pfizer',  'EUA',    TRUE,  now()), " +
            "(100, 'Aché',    'Brasil', TRUE,  now()), " +
            "(100, 'Inativo', NULL,     FALSE, now()), " +
            "(200, 'B-fab',   NULL,     TRUE,  now());");

        await ctx.Database.ExecuteSqlRawAsync(
            "INSERT INTO fornecedores_estoque (estabelecimento_id, razao_social, nome_fantasia, cnpj, " +
            "  contato_nome, contato_telefone, contato_email, prazo_entrega_dias, ativo, criado_em) VALUES " +
            "(100, 'Zeta Distribuidora LTDA', NULL, NULL, NULL, NULL, NULL, 5, TRUE,  now()), " +
            "(100, 'Alpha Med LTDA',          NULL, NULL, NULL, NULL, NULL, 3, TRUE,  now()), " +
            "(100, 'Inativa SA',              NULL, NULL, NULL, NULL, NULL, 7, FALSE, now()), " +
            "(200, 'B-forn LTDA',             NULL, NULL, NULL, NULL, NULL, 1, TRUE,  now());");

        await ctx.Database.ExecuteSqlRawAsync(
            "INSERT INTO locais_estoque (estabelecimento_id, nome, tipo, andar_setor, responsavel, ativo, criado_em) VALUES " +
            "(100, 'Sala 2 - Armário', 0, NULL, NULL, TRUE,  now()), " +
            "(100, 'Geladeira',        0, NULL, NULL, TRUE,  now()), " +
            "(100, 'Local Inativo',    0, NULL, NULL, FALSE, now()), " +
            "(200, 'B-local',          0, NULL, NULL, TRUE,  now());");
#pragma warning restore EF1002

        _sut = new CadastrosEstoqueQueryRepository(
            new AppReadConnectionString(PostgresIntegrationFixture.ConnectionString));
    }

    // ─── Categorias ─────────────────────────────────────────────────────

    [Test]
    public async Task ObterOpcoesCategorias_RetornaApenasAtivasDoEstabelecimento_OrdenadasPorNome()
    {
        var ops = await _sut.ObterOpcoesCategorias(EstabA);

        Assert.That(ops.Count, Is.EqualTo(2),
            "Deve excluir inativa e qualquer registro de outro estabelecimento.");
        Assert.That(ops.Select(o => o.Nome), Is.EqualTo(new[] { "Anestésicos", "Bandagens" }),
            "Ordem alfabética (lowercase).");
        Assert.That(ops.All(o => o.Id > 0), Is.True);
    }

    [Test]
    public async Task ObterOpcoesCategorias_NaoVazaEntreEstabelecimentos()
    {
        var opsB = await _sut.ObterOpcoesCategorias(EstabB);

        Assert.That(opsB.Count, Is.EqualTo(1));
        Assert.That(opsB.Single().Nome, Is.EqualTo("B-categoria"),
            "Defense-in-depth multi-tenant: B só vê o que é dele.");
    }

    // ─── Fabricantes ─────────────────────────────────────────────────────

    [Test]
    public async Task ObterOpcoesFabricantes_RetornaApenasAtivosDoEstabelecimento()
    {
        var ops = await _sut.ObterOpcoesFabricantes(EstabA);

        Assert.That(ops.Select(o => o.Nome), Is.EqualTo(new[] { "Aché", "Pfizer" }));
    }

    [Test]
    public async Task ObterOpcoesFabricantes_NaoVazaEntreEstabelecimentos()
    {
        var opsB = await _sut.ObterOpcoesFabricantes(EstabB);

        Assert.That(opsB.Count, Is.EqualTo(1));
        Assert.That(opsB.Single().Nome, Is.EqualTo("B-fab"));
    }

    // ─── Fornecedores ─────────────────────────────────────────────────────

    [Test]
    public async Task ObterOpcoesFornecedores_RetornaApenasAtivosDoEstabelecimentoOrdenadosPorRazaoSocial()
    {
        var ops = await _sut.ObterOpcoesFornecedores(EstabA);

        Assert.That(ops.Select(o => o.Nome), Is.EqualTo(new[] { "Alpha Med LTDA", "Zeta Distribuidora LTDA" }),
            "Endpoint usa razão social como rótulo (igual ao select do drawer 'Novo produto').");
    }

    [Test]
    public async Task ObterOpcoesFornecedores_NaoVazaEntreEstabelecimentos()
    {
        var opsB = await _sut.ObterOpcoesFornecedores(EstabB);

        Assert.That(opsB.Count, Is.EqualTo(1));
        Assert.That(opsB.Single().Nome, Is.EqualTo("B-forn LTDA"));
    }

    // ─── Locais ─────────────────────────────────────────────────────

    [Test]
    public async Task ObterOpcoesLocais_RetornaApenasAtivosDoEstabelecimento()
    {
        var ops = await _sut.ObterOpcoesLocais(EstabA);

        Assert.That(ops.Select(o => o.Nome), Is.EqualTo(new[] { "Geladeira", "Sala 2 - Armário" }));
    }

    [Test]
    public async Task ObterOpcoesLocais_NaoVazaEntreEstabelecimentos()
    {
        var opsB = await _sut.ObterOpcoesLocais(EstabB);

        Assert.That(opsB.Count, Is.EqualTo(1));
        Assert.That(opsB.Single().Nome, Is.EqualTo("B-local"));
    }

    // ─── LIMIT 500 (sanidade) ─────────────────────────────────────────────

    [Test]
    public async Task ObterOpcoesCategorias_RespeitaLimiteDe500()
    {
        // Insere 600 categorias ativas em EstabA — exigência: query não pode passar de 500.
        await using var conn = new NpgsqlConnection(PostgresIntegrationFixture.ConnectionString);
        await conn.ExecuteAsync(@"
            INSERT INTO categorias_estoque (estabelecimento_id, nome, cor, icone, ativo, criado_em)
            SELECT 100, 'Cat ' || lpad(g::text, 4, '0'), 'hsl(0 70% 50%)', 'fa-box', TRUE, now()
            FROM generate_series(1, 600) g;");

        var ops = await _sut.ObterOpcoesCategorias(EstabA);

        Assert.That(ops.Count, Is.EqualTo(500),
            "LIMIT 500 hardcoded — protege a JSON serialization contra abuso.");
    }
}
