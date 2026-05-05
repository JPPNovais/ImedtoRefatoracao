using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.Test.Helpers;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NUnit.Framework;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// Valida que as CONSTRAINTS reais do Postgres protegem dados quando o app-level
/// falha (defense-in-depth). Esses casos NÃO são pegos por mock — exigem o banco real.
///
/// Cenários:
/// - Unique parcial em SolicitacaoVinculo: dois `Pendente` para o mesmo (prof, estab) → falha.
/// - Unique de Estabelecimentos.dono_usuario_id: dois estabelecimentos para o mesmo dono → falha.
/// - Unique de Estabelecimentos.cnpj: CNPJ duplicado → falha.
/// - Unique parcial de pacientes (estab, cpf): mesmo CPF no mesmo tenant → falha.
/// </summary>
[TestFixture]
public class ConstraintsDbIntegrationTests : IntegrationTestBase
{
    protected override string[] TabelasParaTruncar => new[]
    {
        "solicitacoes_vinculo", "vinculo_profissional_estabelecimento",
        "pacientes", "estabelecimentos", "usuarios"
    };

    [Test]
    public async Task UniqueParcialSolicitacao_DuasPendentesMesmoPar_FalhaNoBanco()
    {
        var profId = Guid.NewGuid();
        const long estabId = 1;

        await using (var ctx = NewContext())
        {
            await SemearEstabelecimentoAsync(ctx, estabId);
            ctx.Set<SolicitacaoVinculo>().Add(SolicitacaoVinculo.Solicitar(profId, estabId, "primeira"));
            await ctx.SaveChangesAsync();
        }

        // Tentar inserir uma segunda pendente diretamente (bypassando o app-level check)
        await using (var ctx = NewContext())
        {
            ctx.Set<SolicitacaoVinculo>().Add(SolicitacaoVinculo.Solicitar(profId, estabId, "segunda"));

            var ex = Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
            // Postgres lança como unique_violation embrulhada em DbUpdateException → InnerException PostgresException
            Assert.That(ex.InnerException, Is.TypeOf<PostgresException>());
            Assert.That(((PostgresException)ex.InnerException!).SqlState, Is.EqualTo("23505"),
                "23505 = unique_violation. Confirma que a unique parcial 'uq_solicitacoes_vinculo_pendente' funciona.");
        }
    }

    [Test]
    public async Task UniqueParcialSolicitacao_PendenteEAprovadaParaMesmoPar_PermitidoPosCancelamento()
    {
        var profId = Guid.NewGuid();
        const long estabId = 1;

        // Primeira: criada e CANCELADA. Segunda: nova pendente — deve passar.
        await using (var ctx = NewContext())
        {
            await SemearEstabelecimentoAsync(ctx, estabId);
            var s1 = SolicitacaoVinculo.Solicitar(profId, estabId, "primeira");
            ctx.Set<SolicitacaoVinculo>().Add(s1);
            await ctx.SaveChangesAsync();
            s1.Cancelar();
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            ctx.Set<SolicitacaoVinculo>().Add(SolicitacaoVinculo.Solicitar(profId, estabId, "segunda"));
            // Constraint é parcial (status = 'Pendente'), então a já cancelada nao colide.
            Assert.DoesNotThrowAsync(() => ctx.SaveChangesAsync());
        }
    }

    [Test]
    public async Task UniqueDono_DuasEstabsMesmoDono_FalhaNoBanco()
    {
        var donoId = Guid.NewGuid();

        await using (var ctx = NewContext())
        {
            var u = Usuario.Criar(donoId, "d@imedto.com");
            u.CompletarOnboarding("D", "11111111111", null);
            ctx.Usuarios.Add(u);
            ctx.Estabelecimentos.Add(Estabelecimento.Criar(donoId, "Primeiro", null, null, null, null));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            ctx.Estabelecimentos.Add(Estabelecimento.Criar(donoId, "Segundo", null, null, null, null));

            var ex = Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
            Assert.That(((PostgresException)ex.InnerException!).SqlState, Is.EqualTo("23505"));
        }
    }

    [Test]
    public async Task UniqueCnpj_MesmoCnpjEmDoisEstabs_FalhaNoBanco()
    {
        const string cnpj = "12345678000195";
        await using (var ctx = NewContext())
        {
            var u1 = Usuario.Criar(Guid.NewGuid(), "d1@imedto.com");
            u1.CompletarOnboarding("D1", "11111111111", null);
            var u2 = Usuario.Criar(Guid.NewGuid(), "d2@imedto.com");
            u2.CompletarOnboarding("D2", "22222222222", null);
            ctx.Usuarios.AddRange(u1, u2);
            ctx.Estabelecimentos.Add(Estabelecimento.Criar(u1.Id, "P", null, cnpj, null, null));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            // u2 tenta criar com mesmo CNPJ (bypassando app-level)
            var u2Id = await ctx.Usuarios
                .Where(u => u.Email == "d2@imedto.com")
                .Select(u => u.Id)
                .SingleAsync();

            ctx.Estabelecimentos.Add(Estabelecimento.Criar(u2Id, "S", null, cnpj, null, null));

            var ex = Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
            Assert.That(((PostgresException)ex.InnerException!).SqlState, Is.EqualTo("23505"));
        }
    }

    [Test]
    public async Task UniqueParcialPacientes_MesmoCpfNoMesmoEstab_FalhaNoBanco()
    {
        const long estabId = 1;
        var cpf = CpfTestData.Validos[0];

        await using (var ctx = NewContext())
        {
            await SemearEstabelecimentoAsync(ctx, estabId);
            ctx.Pacientes.Add(Paciente.Cadastrar(estabId, "P1", cpf, null,
                GeneroPaciente.NaoInformado, null, null, null, null));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            ctx.Pacientes.Add(Paciente.Cadastrar(estabId, "P2", cpf, null,
                GeneroPaciente.NaoInformado, null, null, null, null));

            var ex = Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
            Assert.That(((PostgresException)ex.InnerException!).SqlState, Is.EqualTo("23505"));
        }
    }

    [Test]
    public async Task UniqueParcialPacientes_MesmoCpfEmEstabsDiferentes_Permitido()
    {
        const long estabA = 1;
        const long estabB = 2;
        var cpf = CpfTestData.Validos[1];

        await using (var ctx = NewContext())
        {
            await SemearEstabelecimentoAsync(ctx, estabA);
            await SemearEstabelecimentoAsync(ctx, estabB);
            ctx.Pacientes.Add(Paciente.Cadastrar(estabA, "P1", cpf, null,
                GeneroPaciente.NaoInformado, null, null, null, null));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            ctx.Pacientes.Add(Paciente.Cadastrar(estabB, "P1 Outro Estab", cpf, null,
                GeneroPaciente.NaoInformado, null, null, null, null));

            // Mesmo CPF + estab diferente eh registro independente — index unique parcial
            // (estabelecimento_id, cpf) nao bloqueia.
            Assert.DoesNotThrowAsync(() => ctx.SaveChangesAsync());
        }
    }

    [Test]
    public async Task UniqueParcialPacientes_MesmoDocInternacionalNoMesmoEstab_FalhaNoBanco()
    {
        const long estabId = 1;
        const string doc = "PASSAPORTE-AB123456";

        await using (var ctx = NewContext())
        {
            await SemearEstabelecimentoAsync(ctx, estabId);
            ctx.Pacientes.Add(Paciente.Cadastrar(estabId, "Estrangeiro 1", null, null,
                GeneroPaciente.NaoInformado, null, null, null, null, doc));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            ctx.Pacientes.Add(Paciente.Cadastrar(estabId, "Estrangeiro 2", null, null,
                GeneroPaciente.NaoInformado, null, null, null, null, doc));

            var ex = Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
            Assert.That(((PostgresException)ex.InnerException!).SqlState, Is.EqualTo("23505"),
                "23505 = unique_violation. Confirma que uq_pacientes_estabelecimento_doc_internacional funciona.");
        }
    }

    [Test]
    public async Task UniqueParcialPacientes_MesmoDocInternacionalEmEstabsDiferentes_Permitido()
    {
        const long estabA = 1;
        const long estabB = 2;
        const string doc = "PASSAPORTE-XY999999";

        await using (var ctx = NewContext())
        {
            await SemearEstabelecimentoAsync(ctx, estabA);
            await SemearEstabelecimentoAsync(ctx, estabB);
            ctx.Pacientes.Add(Paciente.Cadastrar(estabA, "P", null, null,
                GeneroPaciente.NaoInformado, null, null, null, null, doc));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            ctx.Pacientes.Add(Paciente.Cadastrar(estabB, "P Outro Estab", null, null,
                GeneroPaciente.NaoInformado, null, null, null, null, doc));

            // Mesma logica do CPF: doc internacional eh por estabelecimento.
            Assert.DoesNotThrowAsync(() => ctx.SaveChangesAsync());
        }
    }

    [Test]
    public async Task UniqueParcialPacientes_DocInternacionalAposSoftDelete_PermiteReuso()
    {
        const long estabId = 1;
        const string doc = "RNE-Z9876543";

        await using (var ctx = NewContext())
        {
            await SemearEstabelecimentoAsync(ctx, estabId);
            var p = Paciente.Cadastrar(estabId, "Antigo", null, null,
                GeneroPaciente.NaoInformado, null, null, null, null, doc);
            ctx.Pacientes.Add(p);
            await ctx.SaveChangesAsync();

            p.MarcarComoDeletado(Guid.NewGuid());
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            ctx.Pacientes.Add(Paciente.Cadastrar(estabId, "Novo com mesmo doc", null, null,
                GeneroPaciente.NaoInformado, null, null, null, null, doc));

            // Index parcial filtra `deletado_em IS NULL` — soft-deleted libera o slot.
            Assert.DoesNotThrowAsync(() => ctx.SaveChangesAsync());
        }
    }

    private static async Task SemearEstabelecimentoAsync(Imedto.Backend.Infrastructure.Database.AppDbContext ctx, long id)
    {
#pragma warning disable EF1002
        await ctx.Database.ExecuteSqlRawAsync(
            $"INSERT INTO estabelecimentos (id, dono_usuario_id, nome_fantasia, status, criado_em, " +
            "  horario_inicio, horario_fim, dias_semana_funcionamento, horarios_bloqueados, datas_bloqueadas) " +
            $"VALUES ({id}, gen_random_uuid(), 'Estab {id}', 0, now(), '08:00', '18:00', '[1,2,3,4,5]'::jsonb, '[]'::jsonb, '[]'::jsonb);");
#pragma warning restore EF1002
    }
}
