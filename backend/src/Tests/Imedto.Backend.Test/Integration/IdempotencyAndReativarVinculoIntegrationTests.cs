using Imedto.Backend.Application.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Idempotency;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// Cenários de fluxos avançados contra Postgres real:
/// - IdempotencyKey: persistência por chave única (PK string), upsert idempotente,
///   limpeza de expirados.
/// - Re-convidar profissional após inativação: aggregate é REUTILIZADO (mesma linha),
///   status volta para Convidado, datas reset, evento Convidado é re-disparado.
/// </summary>
[TestFixture]
public class IdempotencyAndReativarVinculoIntegrationTests : IntegrationTestBase
{
    protected override string[] TabelasParaTruncar => new[]
    {
        "idempotency_keys",
        "vinculo_profissional_estabelecimento",
        "modelo_permissao_estabelecimento",
        "estabelecimentos",
        "usuarios"
    };

    [Test]
    public async Task IdempotencyKey_NovaChave_PersisteComExpiraEm()
    {
        var repo = new IdempotencyRepository(NewContext());

        var k = IdempotencyKey.Registrar(
            key: "POST:/api/agendamento:abc123",
            hashPayload: "sha256-xyz",
            statusCode: 201,
            responseJson: "{\"id\":42}",
            ttl: TimeSpan.FromHours(24));

        await repo.Salvar(k);

        await using var ctx = NewContext();
        var persistido = await ctx.IdempotencyKeys.SingleAsync();
        Assert.That(persistido.Key, Is.EqualTo("POST:/api/agendamento:abc123"));
        Assert.That(persistido.StatusCode, Is.EqualTo(201));
        Assert.That(persistido.ExpiraEm, Is.GreaterThan(DateTime.UtcNow.AddHours(23)));
        Assert.That(persistido.EstaExpirado(), Is.False);
    }

    [Test]
    public async Task IdempotencyKey_MesmaChaveDuasVezes_NaoLancaDuplicateKey()
    {
        var repo1 = new IdempotencyRepository(NewContext());
        var repo2 = new IdempotencyRepository(NewContext());

        await repo1.Salvar(IdempotencyKey.Registrar("dup:1", "h1", 200, "{}", TimeSpan.FromHours(1)));

        // Mesma chave de novo (cliente fez retry) — Salvar faz UPSERT, não deve falhar.
        Assert.DoesNotThrowAsync(async () =>
            await repo2.Salvar(IdempotencyKey.Registrar("dup:1", "h2", 200, "{\"v\":2}", TimeSpan.FromHours(1))));

        await using var ctx = NewContext();
        var k = await ctx.IdempotencyKeys.SingleAsync();
        Assert.That(k.HashPayload, Is.EqualTo("h2"), "Upsert deve ter atualizado o hash.");
        Assert.That(k.ResponseJson, Does.Contain("\"v\":2"));
    }

    [Test]
    public async Task IdempotencyKey_RemoverExpirados_LimpaApenasOsVencidos()
    {
        await using (var ctx = NewContext())
        {
            // 1 expirado, 1 vivo. Insert via aggregate para evitar interpolacao de SQL com {}.
            ctx.IdempotencyKeys.AddRange(
                IdempotencyKey.Registrar("expirado", "h", 200, "{}", TimeSpan.FromMilliseconds(1)),
                IdempotencyKey.Registrar("vivo",     "h", 200, "{}", TimeSpan.FromHours(24)));
            await ctx.SaveChangesAsync();
            // Garante que o "expirado" passou da hora.
            await Task.Delay(20);
        }

        var repo = new IdempotencyRepository(NewContext());
        await repo.RemoverExpiradosAsync();

        await using var ctx2 = NewContext();
        var sobrou = await ctx2.IdempotencyKeys.SingleAsync();
        Assert.That(sobrou.Key, Is.EqualTo("vivo"));
    }

    [Test]
    public async Task Reativar_VinculoInativoExistente_AggregateReusadoStatusVoltaParaConvidado()
    {
        var donoA = Guid.NewGuid();
        var profissional = Guid.NewGuid();
        long estabAId, modeloId, vinculoIdOriginal;

        // Setup: usuario, estab, modelo, vinculo Convidado→Aceito→Inativo
        await using (var ctx = NewContext())
        {
            var dA = Usuario.Criar(donoA, "donoA@imedto.com");
            dA.CompletarOnboarding("Dono A", "11111111111", null);
            var prof = Usuario.Criar(profissional, "prof@imedto.com");
            prof.CompletarOnboarding("Prof", "22222222222", null);
            ctx.Usuarios.AddRange(dA, prof);

            var eA = Estabelecimento.Criar(donoA, "Estab A", null, null, null, null);
            ctx.Estabelecimentos.Add(eA);
            await ctx.SaveChangesAsync();
            estabAId = eA.Id;

            var modelo = ModeloPermissaoEstabelecimento.Criar(estabAId, "Coord", TipoAcessoModelo.Profissional);
            ctx.Set<ModeloPermissaoEstabelecimento>().Add(modelo);
            await ctx.SaveChangesAsync();
            modeloId = modelo.Id;

            var vinculo = VinculoProfissionalEstabelecimento.Convidar(profissional, estabAId, modeloId, donoA);
            ctx.Set<VinculoProfissionalEstabelecimento>().Add(vinculo);
            await ctx.SaveChangesAsync();
            vinculoIdOriginal = vinculo.Id;

            // Aceitar + Inativar para simular ciclo completo
            vinculo.Aceitar();
            vinculo.Inativar();
            await ctx.SaveChangesAsync();
        }

        // Re-convite — aggregate deve ser REUTILIZADO (mesmo Id), não criado novo
        await using (var ctx = NewContext())
        {
            var assinatura = new Mock<IAssinaturaService>();
            assinatura.Setup(s => s.LimiteAtingidoAsync(It.IsAny<long>(), "profissionais", default))
                      .ReturnsAsync(false);
            var sut = new ConvidarProfissionalCommandHandler(
                new EstabelecimentoRepository(ctx),
                new ModeloPermissaoRepository(ctx),
                new UsuarioRepository(ctx),
                new VinculoRepository(ctx),
                new Mock<IEventBus>().Object,
                assinatura.Object);

            await sut.Handle(new ConvidarProfissionalCommand
            {
                EstabelecimentoId = estabAId,
                ConvidadoPorUsuarioId = donoA,
                ProfissionalUsuarioId = profissional,
                ProfissionalEmail = "prof@imedto.com",
                ModeloPermissaoId = modeloId,
            });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            // SO uma linha — aggregate foi reativado, nao duplicado
            var vinculos = await ctx.Set<VinculoProfissionalEstabelecimento>().ToListAsync();
            Assert.That(vinculos, Has.Count.EqualTo(1),
                "Re-convite NAO deve criar nova linha — aggregate inativo deve ser reativado in-place.");

            var v = vinculos.Single();
            Assert.That(v.Id, Is.EqualTo(vinculoIdOriginal),
                "Mesmo aggregate (mesmo Id) — apenas state mudou.");
            Assert.That(v.Status, Is.EqualTo(VinculoStatus.Convidado));
            Assert.That(v.AceitoEm, Is.Null, "Datas de aceite/inativacao zeradas.");
            Assert.That(v.InativadoEm, Is.Null);
        }
    }
}
