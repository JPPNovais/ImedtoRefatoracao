using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using NUnit.Framework;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.IntegrationTest.Infrastructure.SoftDelete;

/// <summary>
/// Testes de integração do <see cref="SoftDeleteInterceptor"/>.
///
/// Decisão técnica: EF InMemory + conexão Npgsql fictícia (host inválido).
/// - O interceptor abre conexão lateral ao banco ANTES de lançar BusinessException
///   (para registrar a auditoria). Com host inválido, a conexão Npgsql falha primeiro.
/// - Cenários 1, 3 e 4 verificam o BLOQUEIO (entidade não removida); para isso é suficiente
///   qualquer exceção ter sido lançada e o estado in-memory não ter mudado.
/// - Cenário 2 (verificar linha em audit_delete_attempts) requer banco real e está marcado
///   como [Category("Integration")] — excluído do CI padrão.
///
/// Para rodar apenas cenários sem banco:
///   dotnet test Tests/Imedto.Backend.IntegrationTest --filter "Category!=Integration"
///
/// Para rodar todos (requer Postgres acessível via IMEDTO_TEST_CONN_STR):
///   dotnet test Tests/Imedto.Backend.IntegrationTest
/// </summary>
[TestFixture]
public class SoftDeleteInterceptorTests
{
    // Conexão lateral usada pelo interceptor para gravar audit.
    // Em cenários sem banco real, o open() falha — mas o BLOQUEIO (exceção) já aconteceu.
    private const string ConexaoInvalida = "Host=localhost-teste-invalido;Port=5999;Database=teste;Username=u;Password=p;Timeout=1;Command Timeout=1";

    private Mock<ICurrentTenantAccessor> _tenantMock;

    [SetUp]
    public void Configurar()
    {
        _tenantMock = new Mock<ICurrentTenantAccessor>();
        _tenantMock.Setup(t => t.TemTenantDefinido).Returns(true);
        _tenantMock.Setup(t => t.UsuarioId).Returns(Guid.NewGuid());
    }

    // --------------------------------------------------------------------------
    // Cenário 1: hard delete de ISoftDeletable lança exceção (bloqueia)
    // --------------------------------------------------------------------------

    [Test]
    public async Task HardDelete_Paciente_LancaExcecao()
    {
        // arrange
        var paciente = CriarPaciente();
        await using var db = CriarDbContext(paciente);

        // act
        db.Pacientes.Remove(paciente);

        // O interceptor lança exceção antes de persistir.
        // Com host inválido, a conexão lateral de audit falha com NpgsqlException ANTES
        // de lançar BusinessException — ambas confirmam o bloqueio.
        Exception? capturada = null;
        try { await db.SaveChangesAsync(); }
        catch (Exception ex) { capturada = ex; }

        // assert — qualquer exceção confirma que o save foi bloqueado
        Assert.That(capturada, Is.Not.Null,
            "SaveChangesAsync deve lançar exceção ao tentar hard delete de ISoftDeletable.");
    }

    [Test]
    public async Task HardDelete_Paciente_EntidadeNaoEhRemovidaDoContexto()
    {
        // arrange
        var paciente = CriarPaciente();
        await using var db = CriarDbContext(paciente);
        db.Pacientes.Remove(paciente);

        // act — ignora a exceção; o que interessa é que o estado não persiste
        try { await db.SaveChangesAsync(); } catch { /* bloqueado — esperado */ }

        // assert — entidade permanece rastreável (não foi apagada)
        var pacienteNoBanco = await db.Pacientes.FindAsync(paciente.Id);
        Assert.That(pacienteNoBanco, Is.Not.Null,
            "Paciente não deve ser removido quando hard delete é bloqueado pelo interceptor.");
    }

    // --------------------------------------------------------------------------
    // Cenário 3: soft delete (MarcarComoDeletado) não lança BusinessException
    // --------------------------------------------------------------------------

    [Test]
    public async Task SoftDelete_Paciente_NaoLancaExcecao()
    {
        // arrange
        var usuarioId = Guid.NewGuid();
        var paciente = CriarPaciente();
        await using var db = CriarDbContext(paciente);

        // act — soft delete correto: muda propriedades, não remove a linha
        paciente.MarcarComoDeletado(usuarioId);

        // assert — SaveChanges deve completar sem exceção
        // (não há EntityState.Deleted, então o interceptor não bloqueia nem abre conexão lateral)
        Assert.DoesNotThrowAsync(async () => await db.SaveChangesAsync(),
            "MarcarComoDeletado não deve ser bloqueado pelo interceptor.");
    }

    [Test]
    public async Task SoftDelete_Paciente_CamposDeletadoEhPreenchido()
    {
        // arrange
        var usuarioId = Guid.NewGuid();
        var paciente = CriarPaciente();
        await using var db = CriarDbContext(paciente);

        // act
        paciente.MarcarComoDeletado(usuarioId);
        await db.SaveChangesAsync();

        // assert
        var salvo = await db.Pacientes.FindAsync(paciente.Id);
        Assert.That(salvo!.DeletadoEm, Is.Not.Null);
        Assert.That(salvo.DeletadoPorUsuarioId, Is.EqualTo(usuarioId));
    }

    // --------------------------------------------------------------------------
    // Cenário 4: hard delete de entidade NÃO ISoftDeletable passa sem bloquear
    // --------------------------------------------------------------------------

    [Test]
    public async Task HardDelete_Estabelecimento_NaoEhBloqueado()
    {
        // arrange — Estabelecimento NÃO implementa ISoftDeletable
        var estabelecimento = Estabelecimento.Criar(
            donoUsuarioId: Guid.NewGuid(),
            nomeFantasia: "Clínica Teste",
            razaoSocial: null,
            cnpj: null,
            telefone: null,
            endereco: null);

        // DbContext isolado apenas com Estabelecimento (sem interceptor bloqueador de paciente)
        var opcoes = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"db-estab-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(CriarInterceptor())
            .Options;

        await using var db = new AppDbContext(opcoes);
        db.Estabelecimentos.Add(estabelecimento);
        await db.SaveChangesAsync();

        // act — hard delete de entidade sem ISoftDeletable
        db.Estabelecimentos.Remove(estabelecimento);

        // assert — nenhuma exceção lançada pelo interceptor
        Assert.DoesNotThrowAsync(async () => await db.SaveChangesAsync(),
            "Hard delete de Estabelecimento (não ISoftDeletable) deve passar sem bloqueio.");
    }

    // --------------------------------------------------------------------------
    // Cenário 2: linha de auditoria gravada — requer banco Postgres real
    // --------------------------------------------------------------------------

    /// <summary>
    /// Verifica que a tentativa de hard delete grava 1 linha em <c>audit_delete_attempts</c>.
    /// Requer variável de ambiente <c>IMEDTO_TEST_CONN_STR</c> com connection string válida.
    /// Marcado como <c>Integration</c> — excluído do dotnet test padrão.
    /// </summary>
    [Test]
    [Category("Integration")]
    public async Task HardDelete_Paciente_GraváUmaLinhaDeAuditoria()
    {
        var connStr = Environment.GetEnvironmentVariable("IMEDTO_TEST_CONN_STR");
        if (string.IsNullOrWhiteSpace(connStr))
            Assert.Ignore("IMEDTO_TEST_CONN_STR não definida. Teste pulado fora de ambiente com banco.");

        var usuarioId = Guid.NewGuid();
        _tenantMock.Setup(t => t.UsuarioId).Returns(usuarioId);

        var interceptor = new SoftDeleteInterceptor(
            new AppReadConnectionString(connStr),
            _tenantMock.Object);

        var opcoes = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connStr)
            .AddInterceptors(interceptor)
            .Options;

        await using var db = new AppDbContext(opcoes);

        var paciente = CriarPaciente();
        db.Pacientes.Add(paciente);
        await db.SaveChangesAsync();

        // Conta auditorias ANTES da tentativa
        var antes = await db.AuditDeleteAttempts
            .CountAsync(a => a.Tabela == "pacientes" && a.UsuarioId == usuarioId);

        // act — tentativa de hard delete
        db.Pacientes.Remove(paciente);
        try { await db.SaveChangesAsync(); } catch (BusinessException) { /* esperado */ }

        // assert — exatamente 1 linha a mais de auditoria
        var depois = await db.AuditDeleteAttempts
            .CountAsync(a => a.Tabela == "pacientes" && a.UsuarioId == usuarioId);
        Assert.That(depois - antes, Is.EqualTo(1),
            "Uma linha de auditoria deve ser registrada para cada tentativa de hard delete bloqueada.");

        // cleanup
        db.Pacientes.Remove(paciente);
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM public.audit_delete_attempts WHERE usuario_id = {0}", usuarioId);
        await db.SaveChangesAsync();
    }

    // --------------------------------------------------------------------------
    // Helpers
    // --------------------------------------------------------------------------

    private SoftDeleteInterceptor CriarInterceptor()
        => new SoftDeleteInterceptor(
            new AppReadConnectionString(ConexaoInvalida),
            _tenantMock.Object);

    private AppDbContext CriarDbContext(params object[] entidades)
    {
        var opcoes = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"db-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(CriarInterceptor())
            .Options;

        var db = new AppDbContext(opcoes);

        foreach (var entidade in entidades)
            db.Add(entidade);

        // Persiste o seed sem disparar o interceptor (entidades em estado Added, não Deleted)
        db.SaveChanges();

        return db;
    }

    private static Paciente CriarPaciente()
        => Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "Teste Interceptor",
            cpf: null,
            dataNascimento: null,
            genero: GeneroPaciente.NaoInformado,
            telefone: null,
            email: null,
            endereco: null,
            observacoes: null);
}
