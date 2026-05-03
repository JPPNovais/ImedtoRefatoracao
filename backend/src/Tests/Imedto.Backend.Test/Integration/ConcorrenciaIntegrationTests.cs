using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Vinculos;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NUnit.Framework;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// Race conditions reais — exigem o banco para serializar/conflituar.
/// Cenários: dois INSERTs simultâneos com unique constraints, dois UPDATEs simultâneos
/// no mesmo aggregate (last-write-wins por padrão sem rowversion).
/// </summary>
[TestFixture]
public class ConcorrenciaIntegrationTests : IntegrationTestBase
{
    protected override string[] TabelasParaTruncar => new[]
    {
        "solicitacoes_vinculo", "vinculo_profissional_estabelecimento",
        "pacientes", "estabelecimentos", "usuarios"
    };

    [Test]
    public async Task RaceCondition_DoisDonosCriandoEstabSimultaneamente_UmGanhaOutroFalhaUniqueViolation()
    {
        // Arrange: 2 donos prontos, ambos vão tentar criar 1 estab cada com mesmo CNPJ.
        const string cnpjConflitante = "12345678000195";
        var dono1 = Guid.NewGuid();
        var dono2 = Guid.NewGuid();

        await using (var ctx = NewContext())
        {
            var u1 = Usuario.Criar(dono1, "d1@imedto.com");
            u1.CompletarOnboarding("D1", "11111111111", null);
            var u2 = Usuario.Criar(dono2, "d2@imedto.com");
            u2.CompletarOnboarding("D2", "22222222222", null);
            ctx.Usuarios.AddRange(u1, u2);
            await ctx.SaveChangesAsync();
        }

        async Task TentarCriar(Guid donoId, string nome)
        {
            await using var ctx = NewContext();
            ctx.Estabelecimentos.Add(Estabelecimento.Criar(donoId, nome, null, cnpjConflitante, null, null));
            await ctx.SaveChangesAsync();
        }

        // Act: dispara as duas em paralelo.
        var t1 = TentarCriar(dono1, "Estab 1");
        var t2 = TentarCriar(dono2, "Estab 2");

        var resultados = await Task.WhenAll(
            t1.ContinueWith(t => t.Exception),
            t2.ContinueWith(t => t.Exception));

        // Assert: exatamente uma das duas deve ter falhado com unique_violation.
        var falhas = resultados.Count(e => e is not null);
        Assert.That(falhas, Is.EqualTo(1),
            "Exatamente uma das requests simultaneas deve ganhar; a outra deve falhar.");

        var excecaoFalha = resultados.Single(e => e is not null)!;
        var dbEx = excecaoFalha.Flatten().InnerExceptions
            .OfType<DbUpdateException>().FirstOrDefault();
        Assert.That(dbEx, Is.Not.Null);
        Assert.That(((PostgresException)dbEx!.InnerException!).SqlState, Is.EqualTo("23505"),
            "Banco deve detectar como unique_violation (CNPJ duplicado).");

        // Estado final: exatamente 1 estabelecimento persistido.
        await using var verifyCtx = NewContext();
        Assert.That(await verifyCtx.Estabelecimentos.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task RaceCondition_DuasSolicitacoesPendentesDoMesmoProfParaMesmoEstab_UmaGanhaOutraFalha()
    {
        const long estabId = 1;
        var profId = Guid.NewGuid();

        await using (var ctx = NewContext())
        {
#pragma warning disable EF1002
            await ctx.Database.ExecuteSqlRawAsync(
                "INSERT INTO estabelecimentos (id, dono_usuario_id, nome_fantasia, status, criado_em, " +
                "  horario_inicio, horario_fim, dias_semana_funcionamento, horarios_bloqueados, datas_bloqueadas) " +
                "VALUES (1, gen_random_uuid(), 'X', 0, now(), '08:00', '18:00', '[1,2,3,4,5]'::jsonb, '[]'::jsonb, '[]'::jsonb);");
#pragma warning restore EF1002
        }

        async Task TentarSolicitar(string mensagem)
        {
            await using var ctx = NewContext();
            ctx.Set<SolicitacaoVinculo>().Add(SolicitacaoVinculo.Solicitar(profId, estabId, mensagem));
            await ctx.SaveChangesAsync();
        }

        var t1 = TentarSolicitar("primeira");
        var t2 = TentarSolicitar("segunda");

        var resultados = await Task.WhenAll(
            t1.ContinueWith(t => t.Exception),
            t2.ContinueWith(t => t.Exception));

        Assert.That(resultados.Count(e => e is not null), Is.EqualTo(1),
            "Apenas uma solicitacao pendente pode existir por par (prof, estab) — outra deve falhar.");

        await using var verifyCtx = NewContext();
        Assert.That(await verifyCtx.Set<SolicitacaoVinculo>().CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task RaceCondition_DoisPacientesMesmoCpfNoMesmoEstab_UmGanhaOutroFalhaUniqueParcial()
    {
        const long estabId = 1;
        const string cpf = "12345678909";

        await using (var ctx = NewContext())
        {
#pragma warning disable EF1002
            await ctx.Database.ExecuteSqlRawAsync(
                "INSERT INTO estabelecimentos (id, dono_usuario_id, nome_fantasia, status, criado_em, " +
                "  horario_inicio, horario_fim, dias_semana_funcionamento, horarios_bloqueados, datas_bloqueadas) " +
                "VALUES (1, gen_random_uuid(), 'X', 0, now(), '08:00', '18:00', '[1,2,3,4,5]'::jsonb, '[]'::jsonb, '[]'::jsonb);");
#pragma warning restore EF1002
        }

        async Task TentarCadastrar(string nome)
        {
            await using var ctx = NewContext();
            ctx.Pacientes.Add(Paciente.Cadastrar(estabId, nome, cpf, null,
                GeneroPaciente.NaoInformado, null, null, null, null));
            await ctx.SaveChangesAsync();
        }

        var t1 = TentarCadastrar("Maria 1");
        var t2 = TentarCadastrar("Maria 2");

        var resultados = await Task.WhenAll(
            t1.ContinueWith(t => t.Exception),
            t2.ContinueWith(t => t.Exception));

        Assert.That(resultados.Count(e => e is not null), Is.EqualTo(1),
            "Apenas um paciente com este CPF pode existir neste estab — race resolvido pela unique parcial.");

        await using var verifyCtx = NewContext();
        Assert.That(await verifyCtx.Pacientes.CountAsync(p => p.Cpf == cpf), Is.EqualTo(1));
    }
}
