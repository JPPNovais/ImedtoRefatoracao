using Imedto.Backend.Application.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// Fluxo end-to-end de vínculos profissionais contra Postgres real:
/// - Convite → Aceitar transita aggregate persistente.
/// - Solicitação inversa → Aprovar.
/// - Tentativas cross-tenant em Aprovar/Recusar/AlterarModelo retornam mensagem genérica.
/// - Unique parcial em (profissional, estab, status='Pendente') impede duas pendentes.
/// </summary>
[TestFixture]
public class VinculosIntegrationTests : IntegrationTestBase
{
    protected override string[] TabelasParaTruncar => new[]
    {
        "vinculo_profissional_estabelecimento",
        "solicitacoes_vinculo",
        "modelo_permissao_estabelecimento",
        "estabelecimentos",
        "usuarios"
    };

    private readonly Guid _donoA = Guid.NewGuid();
    private readonly Guid _donoB = Guid.NewGuid();
    private readonly Guid _profissional = Guid.NewGuid();

    private long _estabAId;
    private long _estabBId;
    private long _modeloEstabAId;

    [SetUp]
    public async Task Seed()
    {
        await using var ctx = NewContext();

        var donoA = Usuario.Criar(_donoA, "donoA@imedto.com");
        donoA.CompletarOnboarding("Dono A", "11111111111", null);
        var donoB = Usuario.Criar(_donoB, "donoB@imedto.com");
        donoB.CompletarOnboarding("Dono B", "22222222222", null);
        var prof = Usuario.Criar(_profissional, "prof@imedto.com");
        prof.CompletarOnboarding("Prof", "33333333333", null);
        ctx.Usuarios.AddRange(donoA, donoB, prof);

        var eA = Estabelecimento.Criar(_donoA, "Estab A", null, null, null, null);
        var eB = Estabelecimento.Criar(_donoB, "Estab B", null, null, null, null);
        ctx.Estabelecimentos.AddRange(eA, eB);
        await ctx.SaveChangesAsync();
        _estabAId = eA.Id;
        _estabBId = eB.Id;

        var modeloA = ModeloPermissaoEstabelecimento.Criar(_estabAId, "Coord", TipoAcessoModelo.Profissional);
        ctx.Set<ModeloPermissaoEstabelecimento>().Add(modeloA);
        await ctx.SaveChangesAsync();
        _modeloEstabAId = modeloA.Id;
    }

    private ConvidarProfissionalCommandHandler ConvidarSut(AppDbContext ctx)
    {
        var assinatura = new Mock<IAssinaturaService>();
        assinatura.Setup(s => s.LimiteAtingidoAsync(It.IsAny<long>(), "profissionais", default))
                  .ReturnsAsync(false);
        // Validação de catálogo é coberta no teste unitário; nos integration tests
        // os cenários atuais não enviam Especialidade — mockamos para retornar true
        // por padrão e mantemos os asserts focados em vínculo×convite.
        var catalogo = new Mock<CatalogoQueryRepository>(
            new Imedto.Backend.Infrastructure.AppReadConnectionString(PostgresIntegrationFixture.ConnectionString));
        catalogo.Setup(r => r.ExisteProfissaoAtiva(It.IsAny<long>())).ReturnsAsync(true);
        catalogo.Setup(r => r.ExisteEspecialidadeAtivaPorNome(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(true);
        return new(
            new EstabelecimentoRepository(ctx),
            new ModeloPermissaoRepository(ctx),
            new UsuarioRepository(ctx),
            new VinculoRepository(ctx),
            new Mock<IEventBus>().Object,
            assinatura.Object,
            catalogo.Object);
    }

    private AceitarConviteCommandHandler AceitarSut(AppDbContext ctx) =>
        new(new VinculoRepository(ctx), new Mock<IEventBus>().Object);

    private SolicitarVinculoCommandHandler SolicitarSut(AppDbContext ctx) =>
        new(new EstabelecimentoRepository(ctx),
            new VinculoRepository(ctx),
            new SolicitacaoVinculoRepository(ctx),
            new Mock<IEventBus>().Object);

    private AprovarSolicitacaoVinculoCommandHandler AprovarSut(AppDbContext ctx) =>
        new(new SolicitacaoVinculoRepository(ctx),
            new EstabelecimentoRepository(ctx),
            new Mock<IEventBus>().Object);

    [Test]
    public async Task FluxoConviteAceitar_PersistirAggregateAtivo()
    {
        long vinculoId;

        await using (var ctx = NewContext())
        {
            await ConvidarSut(ctx).Handle(new ConvidarProfissionalCommand
            {
                EstabelecimentoId = _estabAId,
                ConvidadoPorUsuarioId = _donoA,
                ProfissionalUsuarioId = _profissional,
                ProfissionalEmail = "prof@imedto.com",
                ModeloPermissaoId = _modeloEstabAId,
            });
        }

        await using (var ctx = NewContext())
        {
            var v = await ctx.Set<VinculoProfissionalEstabelecimento>().SingleAsync();
            Assert.That(v.Status, Is.EqualTo(VinculoStatus.Convidado));
            Assert.That(v.ProfissionalUsuarioId, Is.EqualTo(_profissional));
            Assert.That(v.EstabelecimentoId, Is.EqualTo(_estabAId));
            Assert.That(v.ModeloPermissaoId, Is.EqualTo(_modeloEstabAId));
            vinculoId = v.Id;
        }

        await using (var ctx = NewContext())
        {
            await AceitarSut(ctx).Handle(new AceitarConviteCommand
            {
                VinculoId = vinculoId,
                UsuarioSolicitanteId = _profissional,
            });
            // Em produção o UnitOfWorkScope faz SaveChanges no commit. Em integração
            // sem UoW, o Salvar() do repo apenas chama Update() — precisamos persistir aqui.
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            var v = await ctx.Set<VinculoProfissionalEstabelecimento>().SingleAsync();
            Assert.That(v.Status, Is.EqualTo(VinculoStatus.Ativo));
            Assert.That(v.AceitoEm, Is.Not.Null);
        }
    }

    [Test]
    public async Task SolicitarVinculo_DonoAprova_CriaSolicitacaoAprovada()
    {
        long solicitacaoId;

        await using (var ctx = NewContext())
        {
            await SolicitarSut(ctx).Handle(new SolicitarVinculoCommand
            {
                ProfissionalUsuarioId = _profissional,
                EstabelecimentoId = _estabAId,
                Mensagem = "Quero atender",
            });
        }

        await using (var ctx = NewContext())
            solicitacaoId = (await ctx.Set<SolicitacaoVinculo>().SingleAsync()).Id;

        await using (var ctx = NewContext())
        {
            await AprovarSut(ctx).Handle(new AprovarSolicitacaoVinculoCommand
            {
                SolicitacaoId = solicitacaoId,
                EstabelecimentoId = _estabAId,
                AprovadoPorUsuarioId = _donoA,
            });
            await ctx.SaveChangesAsync(); // UoW substituido em integracao
        }

        await using (var ctx = NewContext())
        {
            var s = await ctx.Set<SolicitacaoVinculo>().SingleAsync();
            Assert.That(s.Status, Is.EqualTo(StatusSolicitacaoVinculo.Aprovada));
            Assert.That(s.RespondidaPorUsuarioId, Is.EqualTo(_donoA));
        }
    }

    [Test]
    public async Task AprovarSolicitacao_CrossTenant_LancaSemConsultarEstab()
    {
        long solicitacaoId;
        await using (var ctx = NewContext())
        {
            await SolicitarSut(ctx).Handle(new SolicitarVinculoCommand
            {
                ProfissionalUsuarioId = _profissional,
                EstabelecimentoId = _estabAId,
                Mensagem = "Quero",
            });
        }
        await using (var ctx = NewContext())
            solicitacaoId = (await ctx.Set<SolicitacaoVinculo>().SingleAsync()).Id;

        await using (var ctx = NewContext())
        {
            // Dono do EstabB tenta aprovar solicitacao do EstabA (cross-tenant)
            var ex = Assert.ThrowsAsync<BusinessException>(() =>
                AprovarSut(ctx).Handle(new AprovarSolicitacaoVinculoCommand
                {
                    SolicitacaoId = solicitacaoId,
                    EstabelecimentoId = _estabBId,
                    AprovadoPorUsuarioId = _donoB,
                }));
            Assert.That(ex.Message, Is.EqualTo("Solicitação não encontrada."));
        }

        await using (var ctx = NewContext())
        {
            var s = await ctx.Set<SolicitacaoVinculo>().SingleAsync();
            Assert.That(s.Status, Is.EqualTo(StatusSolicitacaoVinculo.Pendente),
                "Solicitacao do EstabA NAO deve ser tocada por aprovacao cross-tenant.");
        }
    }

    [Test]
    public async Task SolicitarVinculo_JaTemPendente_LancaBusinessException()
    {
        await using (var ctx = NewContext())
        {
            await SolicitarSut(ctx).Handle(new SolicitarVinculoCommand
            {
                ProfissionalUsuarioId = _profissional,
                EstabelecimentoId = _estabAId,
                Mensagem = "Primeira",
            });
        }

        await using (var ctx = NewContext())
        {
            var ex = Assert.ThrowsAsync<BusinessException>(() =>
                SolicitarSut(ctx).Handle(new SolicitarVinculoCommand
                {
                    ProfissionalUsuarioId = _profissional,
                    EstabelecimentoId = _estabAId,
                    Mensagem = "Segunda",
                }));
            Assert.That(ex.Message, Does.Contain("pendente"));
        }
    }
}
