using Imedto.Backend.Application.Termos.Commands;
using Imedto.Backend.Application.Termos.Queries;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Contracts.Termos.Queries;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.Infrastructure.Termos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// End-to-end Postgres real: cria modelo, lista, emite termo, valida multi-tenant.
/// Cobre o ciclo principal definido na spec da Fase 1.
/// </summary>
[TestFixture]
public class TermosIntegrationTests : IntegrationTestBase
{
    protected override string[] TabelasParaTruncar => new[]
    {
        "termo_emitido_acesso_log",
        "termo_audit_log",
        "termo_emitido",
        "termo_modelo_versao",
        "termo_modelo",
        "pacientes",
        "estabelecimentos",
        "usuarios",
    };

    private async Task<(long estabId, Guid donoId)> SeedTenantAsync(string suffix = "A")
    {
        var donoId = Guid.NewGuid();
        await using var ctx = NewContext();
        var u = Usuario.Criar(donoId, $"dono{suffix}@imedto.com");
        u.CompletarOnboarding($"Dono {suffix}", $"1234567890{(int)suffix[0] % 10}", null);
        ctx.Usuarios.Add(u);
        var e = Estabelecimento.Criar(donoId, $"Clinica {suffix}", null, null, null, null);
        ctx.Estabelecimentos.Add(e);
        await ctx.SaveChangesAsync();
        return (e.Id, donoId);
    }

    private async Task<long> SeedPacienteAsync(long estabId, string nome = "Paciente Teste")
    {
        await using var ctx = NewContext();
        var p = Paciente.Cadastrar(estabId, nome, null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);
        ctx.Pacientes.Add(p);
        await ctx.SaveChangesAsync();
        return p.Id;
    }

    [Test]
    public async Task FluxoCompleto_CriarModelo_Listar_Emitir_PersisteSnapshotEHash()
    {
        var (estabId, donoId) = await SeedTenantAsync();
        var pacienteId = await SeedPacienteAsync(estabId);

        // 1. Criar modelo do estabelecimento.
        await using (var ctx = NewContext())
        {
            var sut = new CriarModeloTermoCommandHandler(
                new TermoModeloRepository(ctx),
                new GanssHtmlSanitizer(),
                new EfTermoAuditLogger(ctx, NullLogger<EfTermoAuditLogger>.Instance));

            var cmd = new CriarModeloTermoCommand
            {
                EstabelecimentoId = estabId,
                SolicitanteUsuarioId = donoId,
                Categoria = "lgpd",
                Titulo = "Termo LGPD da clínica",
                ConteudoHtml = "<p>Eu, {{paciente.nome}}, autorizo.</p>",
            };
            await sut.Handle(cmd);
            await ctx.SaveChangesAsync();

            Assert.That(cmd.ModeloIdCriado, Is.GreaterThan(0));
        }

        // 2. Listar modelos.
        await using (var ctx = NewContext())
        {
            var qrepo = new TermoModeloQueryRepository(
                new AppReadConnectionString(PostgresIntegrationFixture.ConnectionString));
            var page = await qrepo.Listar(estabId, null, null, somenteAtivos: true, incluirPadroes: false, 1, 20);
            Assert.That(page.Total, Is.EqualTo(1));
            Assert.That(page.Itens[0].Titulo, Is.EqualTo("Termo LGPD da clínica"));
        }

        // 3. Emitir termo pro paciente.
        long termoId;
        await using (var ctx = NewContext())
        {
            var modelo = await ctx.TermosModelo.SingleAsync();

            var resolver = new TermoResolverDeVariaveis(
                new AppReadConnectionString(PostgresIntegrationFixture.ConnectionString));

            var sut = new EmitirTermoCommandHandler(
                new TermoEmitidoRepository(ctx),
                new TermoModeloRepository(ctx),
                new PacienteRepository(ctx),
                resolver,
                new GanssHtmlSanitizer(),
                new SimpleTermoTextoExtractor(),
                new EfTermoAuditLogger(ctx, NullLogger<EfTermoAuditLogger>.Instance),
                new Mock<IEventBus>().Object);

            var cmd = new EmitirTermoCommand
            {
                PacienteId = pacienteId,
                EstabelecimentoId = estabId,
                EmissorUsuarioId = donoId,
                ModeloId = modelo.Id,
                AssinaturaTipo = "pdf_anexado",
            };
            await sut.Handle(cmd);
            await ctx.SaveChangesAsync();
            termoId = cmd.TermoEmitidoId;
        }

        // 4. Lê termo persistido.
        await using (var ctx = NewContext())
        {
            var t = await ctx.TermosEmitidos.SingleAsync(x => x.Id == termoId);
            Assert.Multiple(() =>
            {
                Assert.That(t.PacienteId, Is.EqualTo(pacienteId));
                Assert.That(t.Status, Is.EqualTo(StatusTermoEmitido.Pendente));
                Assert.That(t.HashIntegridade, Has.Length.EqualTo(64));
                // Snapshot resolvido — paciente "Paciente Teste" foi substituído.
                Assert.That(t.ConteudoSnapshotHtml, Does.Contain("Paciente Teste"));
                Assert.That(t.ConteudoSnapshotHtml, Does.Not.Contain("{{"));
            });
        }
    }

    [Test]
    public async Task ListarModelos_EstabAVeNuncaModeloDoEstabB()
    {
        var (estabA, donoA) = await SeedTenantAsync("X");
        var (estabB, donoB) = await SeedTenantAsync("Y");

        // Modelo só no B.
        await using (var ctx = NewContext())
        {
            var sut = new CriarModeloTermoCommandHandler(
                new TermoModeloRepository(ctx),
                new GanssHtmlSanitizer(),
                new EfTermoAuditLogger(ctx, NullLogger<EfTermoAuditLogger>.Instance));
            await sut.Handle(new CriarModeloTermoCommand
            {
                EstabelecimentoId = estabB,
                SolicitanteUsuarioId = donoB,
                Categoria = "geral",
                Titulo = "Termo do B",
                ConteudoHtml = "<p>x</p>",
            });
            await ctx.SaveChangesAsync();
        }

        var qrepo = new TermoModeloQueryRepository(
            new AppReadConnectionString(PostgresIntegrationFixture.ConnectionString));

        var paginaA = await qrepo.Listar(estabA, null, null, false, false, 1, 20);
        var paginaB = await qrepo.Listar(estabB, null, null, false, false, 1, 20);

        Assert.That(paginaA.Total, Is.EqualTo(0), "Estab A não pode ver modelo do B.");
        Assert.That(paginaB.Total, Is.EqualTo(1));
    }

    [Test]
    public async Task EmitirTermo_PacienteDeOutroTenant_RetornaPacienteNaoEncontrado()
    {
        var (estabA, donoA) = await SeedTenantAsync("M");
        var (estabB, _)     = await SeedTenantAsync("N");
        var pacienteB = await SeedPacienteAsync(estabB, "Paciente do B");

        // Modelo no A.
        long modeloId;
        await using (var ctx = NewContext())
        {
            var sut = new CriarModeloTermoCommandHandler(
                new TermoModeloRepository(ctx),
                new GanssHtmlSanitizer(),
                new EfTermoAuditLogger(ctx, NullLogger<EfTermoAuditLogger>.Instance));
            var cmd = new CriarModeloTermoCommand
            {
                EstabelecimentoId = estabA,
                SolicitanteUsuarioId = donoA,
                Categoria = "geral",
                Titulo = "Termo do A",
                ConteudoHtml = "<p>x</p>",
            };
            await sut.Handle(cmd);
            await ctx.SaveChangesAsync();
            modeloId = cmd.ModeloIdCriado;
        }

        // A tenta emitir pra paciente do B → 404 genérico.
        await using (var ctx = NewContext())
        {
            var sut = new EmitirTermoCommandHandler(
                new TermoEmitidoRepository(ctx),
                new TermoModeloRepository(ctx),
                new PacienteRepository(ctx),
                new TermoResolverDeVariaveis(new AppReadConnectionString(PostgresIntegrationFixture.ConnectionString)),
                new GanssHtmlSanitizer(),
                new SimpleTermoTextoExtractor(),
                new EfTermoAuditLogger(ctx, NullLogger<EfTermoAuditLogger>.Instance),
                new Mock<IEventBus>().Object);

            var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new EmitirTermoCommand
            {
                PacienteId = pacienteB,
                EstabelecimentoId = estabA,
                EmissorUsuarioId = donoA,
                ModeloId = modeloId,
                AssinaturaTipo = "pdf_anexado",
            }));
            Assert.That(ex!.Message, Is.EqualTo("Paciente não encontrado."));
        }
    }

    [Test]
    public async Task RevogarTermo_DeOutroTenant_RetornaTermoNaoEncontrado()
    {
        var (estabA, donoA) = await SeedTenantAsync("R");
        var (estabB, donoB) = await SeedTenantAsync("S");
        var pacienteB = await SeedPacienteAsync(estabB);

        // Emite no B.
        long termoId;
        await using (var ctx = NewContext())
        {
            var modeloB = TermoModelo.CriarDoEstabelecimento(estabB, donoB, CategoriaTermo.Geral, "Termo B", "<p>x</p>");
            ctx.TermosModelo.Add(modeloB);
            await ctx.SaveChangesAsync();

            var termo = TermoEmitido.Emitir(pacienteB, estabB, modeloB.Id, 1, "<p>x</p>", "x",
                AssinaturaTipo.PdfAnexado, donoB, TimeSpan.FromDays(7));
            ctx.TermosEmitidos.Add(termo);
            await ctx.SaveChangesAsync();
            // Assinar pra ficar revogável.
            termo.AnexarPdf("p.pdf", new string('a', 64));
            await ctx.SaveChangesAsync();
            termoId = termo.Id;
        }

        // A tenta revogar termo do B → 404 genérico.
        await using (var ctx = NewContext())
        {
            var sut = new RevogarTermoCommandHandler(
                new TermoEmitidoRepository(ctx),
                new EfTermoAuditLogger(ctx, NullLogger<EfTermoAuditLogger>.Instance),
                new Mock<IEventBus>().Object);

            var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new RevogarTermoCommand
            {
                TermoEmitidoId = termoId,
                EstabelecimentoId = estabA, // outro tenant!
                SolicitanteUsuarioId = donoA,
                Motivo = "tentativa cross-tenant",
            }));
            Assert.That(ex!.Message, Is.EqualTo("Termo não encontrado."));
        }
    }
}
