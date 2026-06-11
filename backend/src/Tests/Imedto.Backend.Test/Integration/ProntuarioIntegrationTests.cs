using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Prontuarios.Pendencias;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// Fluxo end-to-end LGPD de prontuários contra Postgres real:
/// - Iniciar prontuário → registrar evolução → audit trail persistido por escrita.
/// - Cross-tenant em registrar evolução: paciente de outro estab é blocked + nada auditado.
/// - Modelo de outro estab: blocked com mensagem genérica.
/// - Snapshot do modelo é gravado na evolução (preserva forma original mesmo se modelo mudar).
/// </summary>
[TestFixture]
public class ProntuarioIntegrationTests : IntegrationTestBase
{
    protected override string[] TabelasParaTruncar => new[]
    {
        "prontuario_evolucoes",
        "prontuarios",
        "prontuario_acesso_log",
        "modelo_de_prontuario",
        "pacientes",
        "estabelecimentos"
    };

    private const long EstabA = 1;
    private const long EstabB = 2;

    private long _modeloEstabAId;
    private long _modeloEstabBId;
    private long _pacienteEstabAId;
    private long _pacienteEstabBId;

    [SetUp]
    public async Task Seed()
    {
        await using var ctx = NewContext();
#pragma warning disable EF1002
        await ctx.Database.ExecuteSqlRawAsync(
            "INSERT INTO estabelecimentos (id, dono_usuario_id, nome_fantasia, status, criado_em, " +
            "  horario_inicio, horario_fim, dias_semana_funcionamento, horarios_bloqueados, datas_bloqueadas) " +
            "VALUES (1, '11111111-1111-1111-1111-111111111111', 'A', 0, now(), '08:00', '18:00', '[1,2,3,4,5]'::jsonb, '[]'::jsonb, '[]'::jsonb), " +
            "       (2, '22222222-2222-2222-2222-222222222222', 'B', 0, now(), '08:00', '18:00', '[1,2,3,4,5]'::jsonb, '[]'::jsonb, '[]'::jsonb);");
#pragma warning restore EF1002

        var modeloA = ModeloDeProntuario.CriarDoEstabelecimento(EstabA, "Modelo A", null, "{\"campos\":[]}");
        var modeloB = ModeloDeProntuario.CriarDoEstabelecimento(EstabB, "Modelo B", null, "{\"campos\":[]}");
        ctx.Set<ModeloDeProntuario>().AddRange(modeloA, modeloB);

        var pA = Paciente.Cadastrar(EstabA, "Paciente A", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);
        var pB = Paciente.Cadastrar(EstabB, "Paciente B", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);
        ctx.Pacientes.AddRange(pA, pB);

        await ctx.SaveChangesAsync();
        _modeloEstabAId = modeloA.Id;
        _modeloEstabBId = modeloB.Id;
        _pacienteEstabAId = pA.Id;
        _pacienteEstabBId = pB.Id;
    }

    private IniciarProntuarioCommandHandler IniciarSut(AppDbContext ctx) =>
        new(new ProntuarioRepository(ctx),
            new PacienteRepository(ctx),
            new ModeloDeProntuarioRepository(ctx),
            new ProntuarioAcessoLogService(ctx),
            new Mock<IEventBus>().Object);

    private RegistrarEvolucaoCommandHandler RegistrarSut(AppDbContext ctx)
    {
        var pendenciaRepoMock = new Mock<IPendenciaAtendimentoRepository>();
        pendenciaRepoMock.Setup(r => r.ExistePorEvolucaoEAcao(It.IsAny<long>(), It.IsAny<AcaoPendencia>()))
                         .ReturnsAsync(false);
        return new(
            new ProntuarioRepository(ctx),
            new ProntuarioEvolucaoRepository(ctx),
            new PacienteRepository(ctx),
            new ModeloDeProntuarioRepository(ctx),
            new ProntuarioAcessoLogService(ctx),
            new Mock<IEventBus>().Object,
            new PoolExtratorEvolucao(new ProntuarioVariavelPoolRepository(ctx)),
            new PendenciaExtratorEvolucao(pendenciaRepoMock.Object));
    }

    private RegistrarExportacaoProntuarioCommandHandler ExportarProntSut(AppDbContext ctx) =>
        new(new PacienteRepository(ctx),
            new ProntuarioRepository(ctx),
            new ProntuarioAcessoLogService(ctx));

    private RegistrarExportacaoEvolucaoCommandHandler ExportarEvolSut(AppDbContext ctx) =>
        new(new PacienteRepository(ctx),
            new ProntuarioRepository(ctx),
            new ProntuarioEvolucaoRepository(ctx),
            new ProntuarioAcessoLogService(ctx));

    [Test]
    public async Task FluxoIniciarERegistrarEvolucao_PersisteAggregatesEAudit()
    {
        var solicitante = Guid.NewGuid();

        await using (var ctx = NewContext())
        {
            await IniciarSut(ctx).Handle(new IniciarProntuarioCommand
            {
                PacienteId = _pacienteEstabAId,
                EstabelecimentoId = EstabA,
                ModeloDeProntuarioId = _modeloEstabAId,
                SolicitanteUsuarioId = solicitante,
            });
        }

        await using (var ctx = NewContext())
        {
            await RegistrarSut(ctx).Handle(new RegistrarEvolucaoCommand
            {
                PacienteId = _pacienteEstabAId,
                EstabelecimentoId = EstabA,
                AutorUsuarioId = solicitante,
                ConteudoJson = "{\"queixa\":\"dor\"}",
            });
        }

        await using (var ctx = NewContext())
        {
            var prontuario = await ctx.Set<Prontuario>().SingleAsync();
            Assert.That(prontuario.PacienteId, Is.EqualTo(_pacienteEstabAId));
            Assert.That(prontuario.EstabelecimentoId, Is.EqualTo(EstabA));

            var evolucao = await ctx.Set<ProntuarioEvolucao>().SingleAsync();
            Assert.That(evolucao.ProntuarioId, Is.EqualTo(prontuario.Id));
            Assert.That(evolucao.AutorUsuarioId, Is.EqualTo(solicitante));
            // JSONB do Postgres re-serializa (com whitespace) ao persistir — comparar conteudo logico.
            Assert.That(evolucao.ModeloSnapshotJson.Replace(" ", string.Empty),
                Is.EqualTo("{\"campos\":[]}"),
                "Snapshot do modelo deve ser gravado na evolucao para preservar a forma original.");

            var logs = await ctx.Set<ProntuarioAcessoLog>().OrderBy(l => l.OcorridoEm).ToListAsync();
            Assert.That(logs, Has.Count.EqualTo(2),
                "Iniciar + RegistrarEvolucao = 2 audits de Escrita.");
            Assert.That(logs.All(l => l.UsuarioId == solicitante), Is.True);
            Assert.That(logs.All(l => l.TipoAcesso == TipoAcessoProntuario.Escrita), Is.True);
        }
    }

    [Test]
    public async Task RegistrarEvolucao_PacienteCrossTenant_LancaENaoAudita()
    {
        await using (var ctx = NewContext())
        {
            await IniciarSut(ctx).Handle(new IniciarProntuarioCommand
            {
                PacienteId = _pacienteEstabAId,
                EstabelecimentoId = EstabA,
                ModeloDeProntuarioId = _modeloEstabAId,
                SolicitanteUsuarioId = Guid.NewGuid(),
            });
        }

        // EstabB tenta registrar evolução em paciente de EstabA
        await using (var ctx = NewContext())
        {
            var ex = Assert.ThrowsAsync<BusinessException>(() =>
                RegistrarSut(ctx).Handle(new RegistrarEvolucaoCommand
                {
                    PacienteId = _pacienteEstabAId, // paciente de A
                    EstabelecimentoId = EstabB,      // tenant B
                    AutorUsuarioId = Guid.NewGuid(),
                    ConteudoJson = "{}",
                }));
            Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."));
        }

        await using (var ctx = NewContext())
        {
            Assert.That(await ctx.Set<ProntuarioEvolucao>().AnyAsync(), Is.False,
                "Nenhuma evolucao deve ter sido registrada.");
            // Apenas o audit do Iniciar (não o RegistrarEvolucao bloqueado).
            Assert.That(await ctx.Set<ProntuarioAcessoLog>().CountAsync(), Is.EqualTo(1));
        }
    }

    [Test]
    public async Task IniciarProntuario_ModeloDeOutroEstab_LancaMensagemGenerica()
    {
        await using var ctx = NewContext();

        // Paciente de A tentando usar modelo do EstabB (não-padrão sistema)
        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            IniciarSut(ctx).Handle(new IniciarProntuarioCommand
            {
                PacienteId = _pacienteEstabAId,
                EstabelecimentoId = EstabA,
                ModeloDeProntuarioId = _modeloEstabBId, // modelo de B
                SolicitanteUsuarioId = Guid.NewGuid(),
            }));
        Assert.That(ex.Message, Does.Contain("Modelo"));

        Assert.That(await ctx.Set<Prontuario>().AnyAsync(), Is.False);
    }

    [Test]
    public async Task IniciarProntuario_PacienteJaTemProntuario_LancaBusinessException()
    {
        await using (var ctx = NewContext())
        {
            await IniciarSut(ctx).Handle(new IniciarProntuarioCommand
            {
                PacienteId = _pacienteEstabAId,
                EstabelecimentoId = EstabA,
                ModeloDeProntuarioId = _modeloEstabAId,
                SolicitanteUsuarioId = Guid.NewGuid(),
            });
        }

        await using (var ctx = NewContext())
        {
            var ex = Assert.ThrowsAsync<BusinessException>(() =>
                IniciarSut(ctx).Handle(new IniciarProntuarioCommand
                {
                    PacienteId = _pacienteEstabAId,
                    EstabelecimentoId = EstabA,
                    ModeloDeProntuarioId = _modeloEstabAId,
                    SolicitanteUsuarioId = Guid.NewGuid(),
                }));
            Assert.That(ex.Message, Does.Contain("já possui"));
        }

        await using (var ctx = NewContext())
            Assert.That(await ctx.Set<Prontuario>().CountAsync(), Is.EqualTo(1));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Exportação (audit LGPD)
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public async Task RegistrarExportacaoEvolucao_CrossTenant_LancaENaoAudita()
    {
        var solicitante = Guid.NewGuid();

        // Cria prontuário + evolução em EstabA
        await using (var ctx = NewContext())
        {
            await IniciarSut(ctx).Handle(new IniciarProntuarioCommand
            {
                PacienteId = _pacienteEstabAId,
                EstabelecimentoId = EstabA,
                ModeloDeProntuarioId = _modeloEstabAId,
                SolicitanteUsuarioId = solicitante,
            });
        }
        await using (var ctx = NewContext())
        {
            await RegistrarSut(ctx).Handle(new RegistrarEvolucaoCommand
            {
                PacienteId = _pacienteEstabAId,
                EstabelecimentoId = EstabA,
                AutorUsuarioId = solicitante,
                ConteudoJson = "{\"queixa\":\"a\"}",
            });
        }

        long evolucaoId;
        long auditsAntes;
        await using (var ctx = NewContext())
        {
            evolucaoId = (await ctx.Set<ProntuarioEvolucao>().SingleAsync()).Id;
            auditsAntes = await ctx.Set<ProntuarioAcessoLog>().CountAsync();
        }

        // EstabB tenta exportar evolução do paciente de EstabA — deve falhar
        await using (var ctx = NewContext())
        {
            var ex = Assert.ThrowsAsync<BusinessException>(() =>
                ExportarEvolSut(ctx).Handle(new RegistrarExportacaoEvolucaoCommand
                {
                    PacienteId = _pacienteEstabAId,
                    EvolucaoId = evolucaoId,
                    EstabelecimentoId = EstabB,
                    SolicitanteUsuarioId = Guid.NewGuid(),
                }));
            Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."),
                "Mensagem genérica — não revela existência cross-tenant.");
        }

        await using (var ctx = NewContext())
        {
            Assert.That(await ctx.Set<ProntuarioAcessoLog>().CountAsync(), Is.EqualTo(auditsAntes),
                "Tentativa cross-tenant não pode adicionar audit de Exportação.");
        }
    }

    [Test]
    public async Task RegistrarExportacaoEvolucao_EvolucaoDeOutroPaciente_LancaENaoAudita()
    {
        var solicitante = Guid.NewGuid();

        // Cria prontuários + 1 evolução em cada estab
        await using (var ctx = NewContext())
        {
            await IniciarSut(ctx).Handle(new IniciarProntuarioCommand
            {
                PacienteId = _pacienteEstabAId,
                EstabelecimentoId = EstabA,
                ModeloDeProntuarioId = _modeloEstabAId,
                SolicitanteUsuarioId = solicitante,
            });
            await IniciarSut(ctx).Handle(new IniciarProntuarioCommand
            {
                PacienteId = _pacienteEstabBId,
                EstabelecimentoId = EstabB,
                ModeloDeProntuarioId = _modeloEstabBId,
                SolicitanteUsuarioId = solicitante,
            });
        }
        await using (var ctx = NewContext())
        {
            await RegistrarSut(ctx).Handle(new RegistrarEvolucaoCommand
            {
                PacienteId = _pacienteEstabBId,
                EstabelecimentoId = EstabB,
                AutorUsuarioId = solicitante,
                ConteudoJson = "{\"queixa\":\"b\"}",
            });
        }

        long evolucaoDeBId;
        long auditsAntes;
        await using (var ctx = NewContext())
        {
            evolucaoDeBId = (await ctx.Set<ProntuarioEvolucao>().SingleAsync()).Id;
            auditsAntes = await ctx.Set<ProntuarioAcessoLog>().CountAsync();
        }

        // EstabA pede para exportar a evolução de B (que existe, mas é de outro prontuário)
        await using (var ctx = NewContext())
        {
            var ex = Assert.ThrowsAsync<BusinessException>(() =>
                ExportarEvolSut(ctx).Handle(new RegistrarExportacaoEvolucaoCommand
                {
                    PacienteId = _pacienteEstabAId,
                    EvolucaoId = evolucaoDeBId,
                    EstabelecimentoId = EstabA,
                    SolicitanteUsuarioId = solicitante,
                }));
            Assert.That(ex.Message, Is.EqualTo("Evolução não encontrada."));
        }

        await using (var ctx = NewContext())
        {
            Assert.That(await ctx.Set<ProntuarioAcessoLog>().CountAsync(), Is.EqualTo(auditsAntes),
                "Tentativa de exportar evolução fora do prontuário não pode auditar.");
        }
    }

    [Test]
    public async Task RegistrarExportacaoEvolucao_Valida_RegistraAuditExportacao()
    {
        var solicitante = Guid.NewGuid();

        await using (var ctx = NewContext())
        {
            await IniciarSut(ctx).Handle(new IniciarProntuarioCommand
            {
                PacienteId = _pacienteEstabAId,
                EstabelecimentoId = EstabA,
                ModeloDeProntuarioId = _modeloEstabAId,
                SolicitanteUsuarioId = solicitante,
            });
        }
        await using (var ctx = NewContext())
        {
            await RegistrarSut(ctx).Handle(new RegistrarEvolucaoCommand
            {
                PacienteId = _pacienteEstabAId,
                EstabelecimentoId = EstabA,
                AutorUsuarioId = solicitante,
                ConteudoJson = "{\"queixa\":\"a\"}",
            });
        }

        long evolucaoId;
        await using (var ctx = NewContext())
            evolucaoId = (await ctx.Set<ProntuarioEvolucao>().SingleAsync()).Id;

        await using (var ctx = NewContext())
        {
            await ExportarEvolSut(ctx).Handle(new RegistrarExportacaoEvolucaoCommand
            {
                PacienteId = _pacienteEstabAId,
                EvolucaoId = evolucaoId,
                EstabelecimentoId = EstabA,
                SolicitanteUsuarioId = solicitante,
            });
        }

        await using (var ctx = NewContext())
        {
            var exportacoes = await ctx.Set<ProntuarioAcessoLog>()
                .Where(l => l.TipoAcesso == TipoAcessoProntuario.Exportacao)
                .ToListAsync();
            Assert.That(exportacoes, Has.Count.EqualTo(1),
                "Exportação válida deve gerar exatamente 1 audit de Exportacao.");
            Assert.That(exportacoes[0].UsuarioId, Is.EqualTo(solicitante));
            Assert.That(exportacoes[0].EstabelecimentoId, Is.EqualTo(EstabA));
        }
    }

    [Test]
    public async Task RegistrarExportacaoProntuario_CrossTenant_LancaENaoAudita()
    {
        var solicitante = Guid.NewGuid();

        await using (var ctx = NewContext())
        {
            await IniciarSut(ctx).Handle(new IniciarProntuarioCommand
            {
                PacienteId = _pacienteEstabAId,
                EstabelecimentoId = EstabA,
                ModeloDeProntuarioId = _modeloEstabAId,
                SolicitanteUsuarioId = solicitante,
            });
        }

        long auditsAntes;
        await using (var ctx = NewContext())
            auditsAntes = await ctx.Set<ProntuarioAcessoLog>().CountAsync();

        await using (var ctx = NewContext())
        {
            var ex = Assert.ThrowsAsync<BusinessException>(() =>
                ExportarProntSut(ctx).Handle(new RegistrarExportacaoProntuarioCommand
                {
                    PacienteId = _pacienteEstabAId, // paciente de A
                    EstabelecimentoId = EstabB,      // tenant B
                    SolicitanteUsuarioId = Guid.NewGuid(),
                }));
            Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."));
        }

        await using (var ctx = NewContext())
        {
            Assert.That(await ctx.Set<ProntuarioAcessoLog>().CountAsync(), Is.EqualTo(auditsAntes));
        }
    }
}
