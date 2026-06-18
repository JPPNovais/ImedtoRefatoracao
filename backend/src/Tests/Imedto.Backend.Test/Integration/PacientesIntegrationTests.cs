using Imedto.Backend.Application.Pacientes.Commands;
using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// Fluxos LGPD de Pacientes contra Postgres real:
/// - Cadastro + soft-delete persistem nas tabelas certas.
/// - Defense-in-depth: query com tenant filtra de verdade (cross-tenant retorna null).
/// - Audit trail (paciente_acesso_log) é gravado quando exigido.
/// - Mesmo CPF pode existir em estabelecimentos diferentes — único por (cpf, estabelecimento).
/// </summary>
[TestFixture]
public class PacientesIntegrationTests : IntegrationTestBase
{
    protected override string[] TabelasParaTruncar => new[]
    {
        "pacientes", "paciente_acesso_log", "estabelecimentos", "usuarios"
    };

    private const long EstabA = 1;
    private const long EstabB = 2;

    [SetUp]
    public async Task SeedEstabs()
    {
        await using var ctx = NewContext();
        // Insere 2 estabelecimentos via SQL direto (simples, evita usuarios+regras de Criar).
#pragma warning disable EF1002
        await ctx.Database.ExecuteSqlRawAsync(
            "INSERT INTO estabelecimentos (id, dono_usuario_id, nome_fantasia, status, criado_em, " +
            "  horario_inicio, horario_fim, dias_semana_funcionamento, horarios_bloqueados, datas_bloqueadas) " +
            "VALUES (1, '11111111-1111-1111-1111-111111111111', 'Estab A', 0, now(), '08:00', '18:00', '[1,2,3,4,5]'::jsonb, '[]'::jsonb, '[]'::jsonb), " +
            "       (2, '22222222-2222-2222-2222-222222222222', 'Estab B', 0, now(), '08:00', '18:00', '[1,2,3,4,5]'::jsonb, '[]'::jsonb, '[]'::jsonb);");
#pragma warning restore EF1002
    }

    private CadastrarPacienteCommandHandler CadastrarSut(AppDbContext ctx)
    {
        var assinatura = new Mock<IAssinaturaService>();
        assinatura.Setup(s => s.LimiteAtingidoAsync(It.IsAny<long>(), "pacientes", default))
                  .ReturnsAsync(false);
        return new CadastrarPacienteCommandHandler(
            new PacienteRepository(ctx),
            new Mock<IEventBus>().Object,
            assinatura.Object,
            new Mock<IPacienteAcessoLogService>().Object);
    }

    private DeletarPacienteCommandHandler DeletarSut(AppDbContext ctx) =>
        new(new PacienteRepository(ctx),
            new PacienteAcessoLogService(ctx, NullLogger<PacienteAcessoLogService>.Instance, new HttpContextAccessor()));

    private AtualizarPacienteCommandHandler AtualizarSut(AppDbContext ctx) =>
        new(new PacienteRepository(ctx),
            new PacienteAcessoLogService(ctx, NullLogger<PacienteAcessoLogService>.Instance, new HttpContextAccessor()));

    [Test]
    public async Task Cadastrar_PersistePacienteVinculadoAoEstab()
    {
        await using (var ctx = NewContext())
        {
            await CadastrarSut(ctx).Handle(new CadastrarPacienteCommand
            {
                EstabelecimentoId = EstabA,
                NomeCompleto = "Maria Silva",
                Cpf = "12345678909",
                Genero = "Feminino",
            });
        }

        await using (var ctx = NewContext())
        {
            var p = await ctx.Pacientes.SingleAsync();
            Assert.That(p.EstabelecimentoId, Is.EqualTo(EstabA));
            Assert.That(p.Cpf, Is.EqualTo("12345678909"));
            Assert.That(p.Genero, Is.EqualTo(GeneroPaciente.Feminino));
        }
    }

    [Test]
    public async Task Cadastrar_MesmoCpfEmDoisEstabs_AmbosPersistemSemConflito()
    {
        await using (var ctx = NewContext())
        {
            await CadastrarSut(ctx).Handle(new CadastrarPacienteCommand
            {
                EstabelecimentoId = EstabA, NomeCompleto = "Maria", Cpf = "12345678909",
            });
        }
        await using (var ctx = NewContext())
        {
            await CadastrarSut(ctx).Handle(new CadastrarPacienteCommand
            {
                EstabelecimentoId = EstabB, NomeCompleto = "Maria", Cpf = "12345678909",
            });
        }

        await using (var ctx = NewContext())
        {
            var pacientes = await ctx.Pacientes.OrderBy(p => p.EstabelecimentoId).ToListAsync();
            Assert.That(pacientes, Has.Count.EqualTo(2));
            Assert.That(pacientes.Select(p => p.EstabelecimentoId), Is.EqualTo(new[] { EstabA, EstabB }));
        }
    }

    [Test]
    public async Task Cadastrar_CpfDuplicadoNoMesmoEstab_LancaBusinessException()
    {
        await using (var ctx = NewContext())
        {
            await CadastrarSut(ctx).Handle(new CadastrarPacienteCommand
            {
                EstabelecimentoId = EstabA, NomeCompleto = "Maria", Cpf = "12345678909",
            });
        }

        await using (var ctx = NewContext())
        {
            var ex = Assert.ThrowsAsync<BusinessException>(() =>
                CadastrarSut(ctx).Handle(new CadastrarPacienteCommand
                {
                    EstabelecimentoId = EstabA,
                    NomeCompleto = "Outra Maria",
                    Cpf = "12345678909",
                }));
            Assert.That(ex.Message, Does.Contain("CPF"));
        }
    }

    [Test]
    public async Task Atualizar_PacienteDeOutroTenant_LancaMensagemGenericaENaoAlteraDados()
    {
        long pacienteId;
        await using (var ctx = NewContext())
        {
            await CadastrarSut(ctx).Handle(new CadastrarPacienteCommand
            {
                EstabelecimentoId = EstabA, NomeCompleto = "Original", Cpf = "11111111111",
            });
        }
        await using (var ctx = NewContext())
            pacienteId = (await ctx.Pacientes.SingleAsync()).Id;

        // EstabB tenta atualizar paciente de EstabA
        await using (var ctx = NewContext())
        {
            var ex = Assert.ThrowsAsync<BusinessException>(() =>
                AtualizarSut(ctx).Handle(new AtualizarPacienteCommand
                {
                    PacienteId = pacienteId,
                    EstabelecimentoId = EstabB, // tenant diferente do dono
                    SolicitanteUsuarioId = Guid.NewGuid(),
                    NomeCompleto = "HACK",
                    Cpf = "11111111111",
                }));
            Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."));
        }

        await using (var ctx = NewContext())
        {
            var p = await ctx.Pacientes.SingleAsync();
            Assert.That(p.NomeCompleto, Is.EqualTo("Original"),
                "Defense-in-depth: dados do paciente do EstabA NAO mudaram.");
        }

        await using (var ctx = NewContext())
        {
            var logsCount = await ctx.Set<PacienteAcessoLog>().CountAsync();
            Assert.That(logsCount, Is.Zero,
                "Audit nao deve ser registrado em tentativa cross-tenant — caso contrario, atacante poderia inferir existencia.");
        }
    }

    [Test]
    public async Task Deletar_PacienteDoMesmoTenant_SoftDeleteEPersisteAuditLog()
    {
        long pacienteId;
        var solicitante = Guid.NewGuid();

        await using (var ctx = NewContext())
        {
            await CadastrarSut(ctx).Handle(new CadastrarPacienteCommand
            {
                EstabelecimentoId = EstabA, NomeCompleto = "X", Cpf = "11111111111",
            });
        }
        await using (var ctx = NewContext())
            pacienteId = (await ctx.Pacientes.SingleAsync()).Id;

        await using (var ctx = NewContext())
        {
            await DeletarSut(ctx).Handle(new DeletarPacienteCommand
            {
                PacienteId = pacienteId,
                EstabelecimentoId = EstabA,
                SolicitanteUsuarioId = solicitante,
            });
        }

        await using (var ctx = NewContext())
        {
            // Pacientes ainda esta na tabela, mas marcado como deletado.
            var p = await ctx.Pacientes
                .IgnoreQueryFilters()
                .SingleAsync();
            Assert.That(p.DeletadoEm, Is.Not.Null, "Soft-delete devia ter setado DeletadoEm.");
            Assert.That(p.DeletadoPorUsuarioId, Is.EqualTo(solicitante));

            var log = await ctx.Set<PacienteAcessoLog>().SingleAsync();
            Assert.That(log.PacienteId, Is.EqualTo(pacienteId));
            Assert.That(log.UsuarioId, Is.EqualTo(solicitante));
            Assert.That(log.TipoAcesso, Is.EqualTo(TipoAcessoPaciente.Exclusao));
        }
    }

    [Test]
    public async Task Deletar_PacienteCrossTenant_LancaENaoCriaLogDeAcesso()
    {
        long pacienteId;
        await using (var ctx = NewContext())
        {
            await CadastrarSut(ctx).Handle(new CadastrarPacienteCommand
            {
                EstabelecimentoId = EstabA, NomeCompleto = "X", Cpf = "11111111111",
            });
        }
        await using (var ctx = NewContext())
            pacienteId = (await ctx.Pacientes.SingleAsync()).Id;

        await using (var ctx = NewContext())
        {
            Assert.ThrowsAsync<BusinessException>(() =>
                DeletarSut(ctx).Handle(new DeletarPacienteCommand
                {
                    PacienteId = pacienteId,
                    EstabelecimentoId = EstabB, // outro tenant
                    SolicitanteUsuarioId = Guid.NewGuid(),
                }));
        }

        await using (var ctx = NewContext())
        {
            var p = await ctx.Pacientes.IgnoreQueryFilters().SingleAsync();
            Assert.That(p.DeletadoEm, Is.Null, "Paciente original NAO deve ter sido deletado.");
            Assert.That(await ctx.Set<PacienteAcessoLog>().AnyAsync(), Is.False,
                "Sem audit em tentativa cross-tenant.");
        }
    }
}
