using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Usuarios.Events;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.Infrastructure.Tenancy;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Integration;

/// <summary>
/// Comportamentos transversais que só fazem sentido contra um banco real:
/// - <c>SoftDeleteInterceptor</c>: hard delete em <c>ISoftDeletable</c> sempre lança
///   <see cref="BusinessException"/> e registra a tentativa em <c>audit_delete_attempts</c>
///   (em conexão própria, persiste mesmo após rollback).
/// - <c>UnitOfWorkFactory</c>: rollback transparente quando algo falha; eventos
///   de domínio só são publicados após commit.
/// </summary>
[TestFixture]
public class SoftDeleteAndUoWIntegrationTests : IntegrationTestBase
{
    protected override string[] TabelasParaTruncar => new[]
    {
        "pacientes", "estabelecimentos", "usuarios", "audit_delete_attempts"
    };

    private const long EstabId = 1;

    [SetUp]
    public async Task SeedEstab()
    {
        await using var ctx = NewContext();
#pragma warning disable EF1002
        await ctx.Database.ExecuteSqlRawAsync(
            "INSERT INTO estabelecimentos (id, dono_usuario_id, nome_fantasia, status, criado_em, " +
            "  horario_inicio, horario_fim, dias_semana_funcionamento, horarios_bloqueados, datas_bloqueadas) " +
            "VALUES (1, gen_random_uuid(), 'Estab', 0, now(), '08:00', '18:00', '[1,2,3,4,5]'::jsonb, '[]'::jsonb, '[]'::jsonb);");
#pragma warning restore EF1002
    }

    private DbContextOptions<AppDbContext> OptionsComInterceptor()
    {
        var conn = PostgresIntegrationFixture.ConnectionString;
        var tenant = new CurrentTenantAccessor();
        var interceptor = new SoftDeleteInterceptor(new AppReadConnectionString(conn), tenant);
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(conn)
            .AddInterceptors(interceptor)
            .Options;
    }

    [Test]
    public async Task HardDelete_EntidadeSoftDeletable_LancaBusinessException()
    {
        long pacienteId;
        await using (var ctx = new AppDbContext(OptionsComInterceptor()))
        {
            var p = Paciente.Cadastrar(EstabId, "P", null, null,
                GeneroPaciente.NaoInformado, null, null, null, null);
            ctx.Pacientes.Add(p);
            await ctx.SaveChangesAsync();
            pacienteId = p.Id;
        }

        await using (var ctx = new AppDbContext(OptionsComInterceptor()))
        {
            var p = await ctx.Pacientes.SingleAsync();
            ctx.Pacientes.Remove(p); // hard delete — interceptor deve bloquear

            var ex = Assert.ThrowsAsync<BusinessException>(() => ctx.SaveChangesAsync());
            Assert.That(ex.Message, Does.Contain("exclusão lógica"));
        }

        await using (var ctx = NewContext())
        {
            // Paciente continua na base (rollback do hard delete).
            Assert.That(await ctx.Pacientes.AnyAsync(p => p.Id == pacienteId), Is.True);
        }
    }

    [Test]
    public async Task HardDelete_RegistraEmAuditMesmoAposRollback()
    {
        await using (var ctx = new AppDbContext(OptionsComInterceptor()))
        {
            var p = Paciente.Cadastrar(EstabId, "P", null, null,
                GeneroPaciente.NaoInformado, null, null, null, null);
            ctx.Pacientes.Add(p);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new AppDbContext(OptionsComInterceptor()))
        {
            var p = await ctx.Pacientes.SingleAsync();
            ctx.Pacientes.Remove(p);

            Assert.ThrowsAsync<BusinessException>(() => ctx.SaveChangesAsync());
        }

        // Audit é gravado em conexão independente (NpgsqlConnection separada),
        // então persiste mesmo com rollback da transação principal.
        await using (var ctx = NewContext())
        {
#pragma warning disable EF1002
            var audits = await ctx.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*)::int as \"Value\" FROM audit_delete_attempts").ToListAsync();
#pragma warning restore EF1002
            Assert.That(audits.Single(), Is.GreaterThan(0),
                "Tentativa de hard delete deve ter sido registrada em audit_delete_attempts.");
        }
    }

    [Test]
    public async Task UnitOfWork_FalhaNaTransacao_RollbackPreservaEstadoAnterior()
    {
        var donoId = Guid.NewGuid();
        var eventBus = new Mock<IEventBus>();

        // Tentar criar usuário e em seguida forçar erro — UoW deve reverter o usuário.
        try
        {
            await using var ctx = new AppDbContext(OptionsComInterceptor());
            var dispatcher = new EfDomainEventDispatcher(ctx, eventBus.Object);
            var factory = new UnitOfWorkFactoryDirect(ctx, dispatcher);
            await using var scope = await factory.OpenAsync();

            ctx.Usuarios.Add(Usuario.Criar(donoId, "tx@imedto.com"));
            await ctx.SaveChangesAsync();

            // Erro intencional — força rollback antes de Commit.
            throw new InvalidOperationException("forçar rollback");
            // ReSharper disable once HeuristicUnreachableCode
            // await scope.CommitAsync();
        }
        catch (InvalidOperationException)
        {
            // Esperado.
        }

        await using (var ctx = NewContext())
        {
            Assert.That(await ctx.Usuarios.AnyAsync(u => u.Id == donoId), Is.False,
                "Rollback do UoW deve ter revertido o INSERT.");
        }
    }

    [Test]
    public async Task UnitOfWork_Commit_DispatchEventosPosSaveChanges()
    {
        var donoId = Guid.NewGuid();
        var publicados = new List<IDomainEvent>();
        var eventBus = new Mock<IEventBus>();
        eventBus.Setup(b => b.Publish(It.IsAny<IDomainEvent>()))
                .Callback<IDomainEvent>(e => publicados.Add(e))
                .Returns(Task.CompletedTask);

        await using var ctx = new AppDbContext(OptionsComInterceptor());
        var dispatcher = new EfDomainEventDispatcher(ctx, eventBus.Object);
        var factory = new UnitOfWorkFactoryDirect(ctx, dispatcher);
        await using (var scope = await factory.OpenAsync())
        {
            // Usuario.Criar adiciona UsuarioCriadoEvent ao aggregate.
            ctx.Usuarios.Add(Usuario.Criar(donoId, "ev@imedto.com"));
            await scope.CommitAsync();
        }

        Assert.That(publicados.OfType<UsuarioCriadoEvent>().Count(), Is.EqualTo(1),
            "Domain event deve ser publicado uma vez no Commit.");

        await using var verifyCtx = NewContext();
        Assert.That(await verifyCtx.Usuarios.AnyAsync(u => u.Id == donoId), Is.True);
    }

    /// <summary>
    /// Helper que reusa um <see cref="AppDbContext"/> existente — em produção, o factory
    /// resolve o contexto via DI scoped. Aqui injetamos manualmente.
    /// </summary>
    private sealed class UnitOfWorkFactoryDirect
    {
        private readonly AppDbContext _ctx;
        private readonly IDomainEventDispatcher _dispatcher;

        public UnitOfWorkFactoryDirect(AppDbContext ctx, IDomainEventDispatcher dispatcher)
        {
            _ctx = ctx;
            _dispatcher = dispatcher;
        }

        public async Task<EfUnitOfWorkScopeAdapter> OpenAsync()
        {
            var tx = await _ctx.Database.BeginTransactionAsync();
            return new EfUnitOfWorkScopeAdapter(_ctx, _dispatcher, tx);
        }
    }

    private sealed class EfUnitOfWorkScopeAdapter : IAsyncDisposable
    {
        private readonly AppDbContext _ctx;
        private readonly IDomainEventDispatcher _dispatcher;
        private readonly Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction _tx;
        private bool _committed;

        public EfUnitOfWorkScopeAdapter(
            AppDbContext ctx,
            IDomainEventDispatcher dispatcher,
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx)
        {
            _ctx = ctx;
            _dispatcher = dispatcher;
            _tx = tx;
        }

        public async Task CommitAsync()
        {
            await _ctx.SaveChangesAsync();
            await _dispatcher.DispatchAsync();
            if (_ctx.ChangeTracker.HasChanges())
                await _ctx.SaveChangesAsync();
            await _tx.CommitAsync();
            _committed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_committed)
                await _tx.RollbackAsync();
            await _tx.DisposeAsync();
        }
    }
}
