using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Termos;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// Cobre a query Dapper que faltou regression antes do bug P0 de 2026-05-19
/// (deploy 837b50d): <c>p.id</c> referenciado em <c>public.profissionais</c>,
/// cuja PK é <c>usuario_id</c>. Postgres real é obrigatório aqui — o teste
/// unitário com Mock<ITermoResolverDeVariaveis> não pega esse tipo de erro.
///
/// Cobre também o filtro multi-tenant via <c>vinculo_profissional_estabelecimento</c>:
/// profissional só resolve quando tem vínculo Ativo no estabelecimento corrente.
/// </summary>
[TestFixture]
public class TermoResolverDeVariaveisIntegrationTests : IntegrationTestBase
{
    protected override string[] TabelasParaTruncar => new[]
    {
        "vinculo_profissional_estabelecimento",
        "profissionais",
        "pacientes",
        "estabelecimentos",
        "usuarios",
    };

    private const long EstabA = 100;
    private const long EstabB = 200;
    private const long PacienteId = 500;
    private static readonly Guid ProfissionalUsuarioId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DonoBUsuarioId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private TermoResolverDeVariaveis _sut;

    [SetUp]
    public async Task Seed()
    {
        await using var ctx = NewContext();
#pragma warning disable EF1002
        await ctx.Database.ExecuteSqlRawAsync($"""
            INSERT INTO estabelecimentos (id, dono_usuario_id, nome_fantasia, status, criado_em,
                horario_inicio, horario_fim, dias_semana_funcionamento, horarios_bloqueados, datas_bloqueadas)
            VALUES
              ({EstabA}, '{ProfissionalUsuarioId}', 'Clinica A', 0, now(),
                '08:00', '18:00', '[1,2,3,4,5]'::jsonb, '[]'::jsonb, '[]'::jsonb),
              ({EstabB}, '{DonoBUsuarioId}', 'Clinica B', 0, now(),
                '08:00', '18:00', '[1,2,3,4,5]'::jsonb, '[]'::jsonb, '[]'::jsonb);

            INSERT INTO usuarios (id, nome_completo, email, criado_em, primeiro_acesso, mfa_ativo)
            VALUES
              ('{ProfissionalUsuarioId}', 'Dra. Mariana Costa', 'mariana@a.local', now(), TRUE, FALSE);

            INSERT INTO profissionais (usuario_id, conselho, uf, numero_registro, especialidade, criado_em)
            VALUES
              ('{ProfissionalUsuarioId}', 'CRM', 'SP', '12345', 'Cardiologia', now());

            INSERT INTO pacientes (id, estabelecimento_id, nome_completo, genero, criado_em)
            VALUES
              ({PacienteId}, {EstabA}, 'Paciente Alfa', 'NaoInformado', now());
            """);
#pragma warning restore EF1002

        _sut = new TermoResolverDeVariaveis(
            new AppReadConnectionString(PostgresIntegrationFixture.ConnectionString));
    }

    private async Task SeedVinculoAtivoAsync(Guid profissional, long estab)
    {
        await using var ctx = NewContext();
#pragma warning disable EF1002
        await ctx.Database.ExecuteSqlRawAsync($"""
            INSERT INTO vinculo_profissional_estabelecimento
              (profissional_usuario_id, estabelecimento_id, convidado_por_usuario_id, status, convidado_em, aceito_em)
            VALUES
              ('{profissional}', {estab}, '{DonoBUsuarioId}', 'Ativo', now(), now());
            """);
#pragma warning restore EF1002
    }

    [Test]
    public async Task ResolverAsync_ProfissionalSemVinculo_CaiNoFallback()
    {
        // Nenhum vínculo inserido — o profissional existe na tabela mas não pode atuar em A.
        const string html = "<p>Dr(a). {{profissional.nome}} - {{profissional.conselho_completo}}</p>";
        var ctx = new ContextoDeVariaveis(PacienteId, EstabA, ProfissionalUsuarioId);

        var resultado = await _sut.ResolverAsync(html, ctx);

        Assert.That(resultado, Does.Contain("___________"),
            "Sem vínculo ativo, profissional.nome deve cair no fallback.");
        Assert.That(resultado, Does.Not.Contain("Mariana"));
        Assert.That(resultado, Does.Not.Contain("CRM"));
    }

    [Test]
    public async Task ResolverAsync_ProfissionalComVinculoAtivoNoEstab_PreencheVariaveis()
    {
        await SeedVinculoAtivoAsync(ProfissionalUsuarioId, EstabA);

        const string html = "<p>Dr(a). {{profissional.nome}} - {{profissional.conselho_completo}} ({{profissional.especialidade}})</p>";
        var ctx = new ContextoDeVariaveis(PacienteId, EstabA, ProfissionalUsuarioId);

        var resultado = await _sut.ResolverAsync(html, ctx);

        Assert.That(resultado, Does.Contain("Dra. Mariana Costa"));
        Assert.That(resultado, Does.Contain("CRM-SP 12345"));
        Assert.That(resultado, Does.Contain("Cardiologia"));
    }

    [Test]
    public async Task ResolverAsync_ProfissionalAtivoEmOutroEstab_NaoVazaCrossTenant()
    {
        // Vínculo Ativo no estab B, mas a resolução é pedida no contexto de A → não pode vazar.
        await SeedVinculoAtivoAsync(ProfissionalUsuarioId, EstabB);

        const string html = "<p>{{profissional.nome}} / {{profissional.conselho_completo}}</p>";
        var ctx = new ContextoDeVariaveis(PacienteId, EstabA, ProfissionalUsuarioId);

        var resultado = await _sut.ResolverAsync(html, ctx);

        Assert.That(resultado, Does.Not.Contain("Mariana"),
            "Defense-in-depth multi-tenant: profissional vinculado a B não pode aparecer em termo de A.");
        Assert.That(resultado, Does.Not.Contain("CRM-SP"));
        Assert.That(resultado, Does.Contain("___________"));
    }

    [Test]
    public async Task ResolverAsync_SemProfissionalUsuarioId_NaoConsultaTabelaProfissionais()
    {
        // ProfissionalUsuarioId = null → resolver pula a query, todas as variáveis caem no fallback.
        const string html = "<p>{{profissional.nome}} - {{paciente.nome}}</p>";
        var ctx = new ContextoDeVariaveis(PacienteId, EstabA, null);

        var resultado = await _sut.ResolverAsync(html, ctx);

        Assert.That(resultado, Does.Contain("Paciente Alfa"));
        Assert.That(resultado, Does.Contain("___________"));
    }
}
