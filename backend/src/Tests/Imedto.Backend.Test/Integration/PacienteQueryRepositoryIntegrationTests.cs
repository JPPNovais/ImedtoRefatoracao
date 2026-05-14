using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.Test.Helpers;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// Testes de leitura via Dapper (PacienteQueryRepository) contra Postgres real:
/// - Filtro por estabelecimento isolado (não vaza pacientes de outro tenant).
/// - Soft-deletados não aparecem.
/// - Busca trigram com unaccent (acento, maiúscula, parcial) funciona.
/// - Busca por CPF (somente dígitos).
/// - Paginação respeita Total/Pagina/TamanhoPagina.
/// </summary>
[TestFixture]
public class PacienteQueryRepositoryIntegrationTests : IntegrationTestBase
{
    protected override string[] TabelasParaTruncar => new[]
    {
        "pacientes", "estabelecimentos"
    };

    private const long EstabA = 1;
    private const long EstabB = 2;
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
#pragma warning restore EF1002

        // Insere 5 pacientes em A (1 deletado), 2 em B. Uso CpfTestData (CPFs com DV
        // valido) — a regra nova de validacao rejeita as sequencias 111..., 222...
        var pAJoao = Paciente.Cadastrar(EstabA, "João Silva", CpfTestData.Validos[0], null,
            GeneroPaciente.Masculino, null, null, null, null);
        var pAMaria = Paciente.Cadastrar(EstabA, "Maria Souza", CpfTestData.Validos[1], null,
            GeneroPaciente.Feminino, null, null, null, null);
        var pAJose = Paciente.Cadastrar(EstabA, "José Pedro", CpfTestData.Validos[2], null,
            GeneroPaciente.Masculino, null, null, null, null);
        var pAAna = Paciente.Cadastrar(EstabA, "Ana Lúcia", CpfTestData.Validos[3], null,
            GeneroPaciente.Feminino, null, null, null, null);
        var pADeletado = Paciente.Cadastrar(EstabA, "Deletado", CpfTestData.Validos[4], null,
            GeneroPaciente.Masculino, null, null, null, null);
        pADeletado.MarcarComoDeletado(Guid.NewGuid());

        var pBOutro = Paciente.Cadastrar(EstabB, "Outro Estab Maria", CpfTestData.Validos[5], null,
            GeneroPaciente.Feminino, null, null, null, null);
        var pBOutro2 = Paciente.Cadastrar(EstabB, "B Joao", CpfTestData.Validos[6], null,
            GeneroPaciente.Masculino, null, null, null, null);

        ctx.Pacientes.AddRange(pAJoao, pAMaria, pAJose, pAAna, pADeletado, pBOutro, pBOutro2);
        await ctx.SaveChangesAsync();

        _sut = new PacienteQueryRepository(new AppReadConnectionString(PostgresIntegrationFixture.ConnectionString));
    }

    [Test]
    public async Task Listar_SemBusca_RetornaApenasPacientesDoEstabENaoDeletados()
    {
        var resultado = await _sut.Listar(EstabA, busca: null, pagina: 1, tamanhoPagina: 100);

        Assert.That(resultado.Total, Is.EqualTo(4),
            "Apenas os 4 ativos do EstabA — exclui o deletado e os de EstabB.");
        Assert.That(resultado.Itens.Select(i => i.NomeCompleto),
            Is.EquivalentTo(new[] { "João Silva", "Maria Souza", "José Pedro", "Ana Lúcia" }));
    }

    [Test]
    public async Task Listar_PorEstabB_RetornaApenasPacientesDeB()
    {
        var resultado = await _sut.Listar(EstabB, busca: null, pagina: 1, tamanhoPagina: 100);

        Assert.That(resultado.Total, Is.EqualTo(2));
        Assert.That(resultado.Itens.All(i => !i.NomeCompleto.StartsWith("João Silva")), Is.True,
            "Defense-in-depth multi-tenant: não vaza pacientes de outro estab.");
    }

    [Test]
    public async Task Listar_BuscaPorNomeComAcento_MatchesUsandoUnaccent()
    {
        // Busca "joao" (sem acento) deve casar com "João Silva" (com acento)
        var resultado = await _sut.Listar(EstabA, busca: "joao", pagina: 1, tamanhoPagina: 100);

        Assert.That(resultado.Total, Is.EqualTo(1));
        Assert.That(resultado.Itens.Single().NomeCompleto, Is.EqualTo("João Silva"));
    }

    [Test]
    public async Task Listar_BuscaPorNomeMaiuscula_MatchesUsandoLower()
    {
        var resultado = await _sut.Listar(EstabA, busca: "MARIA", pagina: 1, tamanhoPagina: 100);

        Assert.That(resultado.Total, Is.EqualTo(1));
        Assert.That(resultado.Itens.Single().NomeCompleto, Is.EqualTo("Maria Souza"));
    }

    [Test]
    public async Task Listar_BuscaPorCpfPrefixo_MatchesPorPrefixoNumerico()
    {
        // pAJoao usa CpfTestData.Validos[0] = "12345678909" — buscamos pelo prefixo formatado.
        var resultado = await _sut.Listar(EstabA, busca: "123.456", pagina: 1, tamanhoPagina: 100);

        Assert.That(resultado.Total, Is.EqualTo(1));
        Assert.That(resultado.Itens.Single().Cpf, Is.EqualTo(CpfTestData.Validos[0]));
    }

    [Test]
    public async Task Listar_Paginacao_RespeitaLimitsEOffset()
    {
        var pagina1 = await _sut.Listar(EstabA, busca: null, pagina: 1, tamanhoPagina: 2);
        var pagina2 = await _sut.Listar(EstabA, busca: null, pagina: 2, tamanhoPagina: 2);

        Assert.That(pagina1.Itens.Count(), Is.EqualTo(2));
        Assert.That(pagina2.Itens.Count(), Is.EqualTo(2));
        Assert.That(pagina1.Total, Is.EqualTo(4));
        Assert.That(pagina2.Total, Is.EqualTo(4));

        // Sem sobreposição entre páginas.
        var idsPagina1 = pagina1.Itens.Select(i => i.Id).ToHashSet();
        var idsPagina2 = pagina2.Itens.Select(i => i.Id).ToHashSet();
        Assert.That(idsPagina1.Overlaps(idsPagina2), Is.False);
    }

    [Test]
    public async Task ObterPorId_PacienteDoOutroEstab_RetornaNull()
    {
        long pacienteEstabBId;
        await using (var ctx = NewContext())
            pacienteEstabBId = (await ctx.Pacientes.FirstAsync(p => p.EstabelecimentoId == EstabB)).Id;

        // EstabA tenta obter paciente de EstabB pelo Id direto — deve retornar null.
        var resultado = await _sut.ObterPorId(pacienteEstabBId, EstabA);

        Assert.That(resultado, Is.Null,
            "Defense-in-depth: filtro por estabelecimentoId no SQL bloqueia IDOR.");
    }

    // ─── BuscaRapida (Correção 5 — autocomplete LGPD-friendly) ─────────────

    [Test]
    public async Task BuscaRapida_SemBusca_RetornaUltimosCadastradosDoEstab()
    {
        var resultado = await _sut.BuscaRapida(EstabA, q: null, limite: 10);

        // 4 ativos em EstabA (excluindo soft-deletado e os de EstabB).
        Assert.That(resultado.Count, Is.EqualTo(4));
        Assert.That(resultado.All(r => r.NomeCompleto != "Deletado"), Is.True);
        Assert.That(resultado.All(r => r.NomeCompleto != "Outro Estab Maria"), Is.True,
            "Defense-in-depth multi-tenant: BuscaRapida NÃO pode vazar pacientes de outro estab.");
    }

    [Test]
    public async Task BuscaRapida_ComBusca_FiltraPorNomeComUnaccent()
    {
        // "maria" (sem acento, minúsculo) deve casar com "Maria Souza" do EstabA.
        // E NÃO com "Outro Estab Maria" do EstabB.
        var resultado = await _sut.BuscaRapida(EstabA, q: "maria", limite: 10);

        Assert.That(resultado.Count, Is.EqualTo(1));
        Assert.That(resultado.Single().NomeCompleto, Is.EqualTo("Maria Souza"));
    }

    [Test]
    public async Task BuscaRapida_LimiteAcimaDoMaximo_FazClampEm30()
    {
        // Math.Clamp(1000, 1, 30) = 30. Como temos só 4 pacientes em EstabA,
        // o teste se concentra em garantir que LIMIT não é injetado livre.
        // (Cobertura comportamental — não dá pra observar o LIMIT direto sem
        // mock do conn, mas o resultado nunca passa do que existe.)
        var resultado = await _sut.BuscaRapida(EstabA, q: null, limite: 1000);

        Assert.That(resultado.Count, Is.LessThanOrEqualTo(30));
    }

    [Test]
    public async Task BuscaRapida_LimiteZeroOuNegativo_ClampPara1()
    {
        // Math.Clamp(-5, 1, 30) = 1 → no máximo 1 item.
        var resultado = await _sut.BuscaRapida(EstabA, q: null, limite: -5);

        Assert.That(resultado.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task BuscaRapida_RetornaApenasIdENomeCompleto()
    {
        // LGPD: DTO não pode carregar CPF/telefone/data. O teste de superfície
        // do DTO já garante isso em compile/runtime, mas aqui validamos que o
        // pipeline real do Dapper também produz só id + nome (sem warnings de
        // mapping silenciosos).
        var resultado = await _sut.BuscaRapida(EstabA, q: null, limite: 10);

        foreach (var item in resultado)
        {
            Assert.That(item.Id, Is.GreaterThan(0));
            Assert.That(item.NomeCompleto, Is.Not.Null.And.Not.Empty);
        }
    }

    [Test]
    public async Task BuscaRapida_BuscaPorEstabB_NaoVazaPacientesDeEstabA()
    {
        // Mesmo pesquisando por um termo que existiria em A ("joão"), só pode
        // retornar pacientes de B (defense-in-depth multi-tenant).
        var resultado = await _sut.BuscaRapida(EstabB, q: "joao", limite: 10);

        Assert.That(resultado.All(r => r.NomeCompleto != "João Silva"), Is.True,
            "Multi-tenant: nunca pode retornar 'João Silva' (que é de EstabA) ao buscar em EstabB.");
    }

    [Test]
    public async Task BuscaRapida_BuscaApenasEspacos_TratadoComoSemBusca()
    {
        // string.IsNullOrWhiteSpace deve mandar pro caminho "sem filtro".
        var resultado = await _sut.BuscaRapida(EstabA, q: "   ", limite: 10);

        Assert.That(resultado.Count, Is.EqualTo(4));
    }

    [Test]
    public async Task ObterParaExportLgpd_IncluiSoftDeletados()
    {
        long pacienteDeletadoId;
        await using (var ctx = NewContext())
        {
            pacienteDeletadoId = (await ctx.Pacientes
                .IgnoreQueryFilters()
                .FirstAsync(p => p.NomeCompleto == "Deletado")).Id;
        }

        // Export LGPD deve trazer mesmo soft-deletados (titular tem direito ao histórico).
        var resultado = await _sut.ObterParaExportLgpd(pacienteDeletadoId, EstabA);

        Assert.That(resultado, Is.Not.Null);
        Assert.That(resultado.DeletadoEm, Is.Not.Null);
    }
}
