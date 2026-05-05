using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Dapper;
using Npgsql;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Infrastructure.Database;

/// <summary>
/// Bloqueia tentativas de hard delete em entidades <see cref="ISoftDeletable"/> e
/// registra cada tentativa em <c>audit_delete_attempts</c> (em conexão independente,
/// para que o registro persista mesmo após o rollback da transação principal).
///
/// Uso correto: chamar <c>aggregate.MarcarComoDeletado(usuarioId)</c> + <c>repo.Salvar(...)</c>.
/// Hard delete (<c>DbSet.Remove</c>, <c>EntityState.Deleted</c>) sempre lança
/// <see cref="BusinessException"/> e não chega a executar.
///
/// Compatível com <c>AddDbContextPool</c>: o tenant accessor (scoped) NUNCA é capturado
/// pelo construtor — seria uma captive dependency, já que o interceptor vive enquanto a
/// instância pooled do DbContext for reutilizada entre requests. Em vez disso, o accessor
/// é resolvido por-request via <see cref="IHttpContextAccessor"/>. Sem HttpContext (jobs
/// em background, testes que não passam accessor explícito), <c>UsuarioId</c> fica nulo no
/// audit — o registro do hard delete continua sendo gravado.
/// </summary>
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly AppReadConnectionString _connectionString;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ICurrentTenantAccessor? _tenantOverride;

    /// <summary>
    /// Construtor de produção (singleton-safe). O tenant accessor é resolvido a cada
    /// SaveChanges via <see cref="IHttpContextAccessor.HttpContext"/>.RequestServices.
    /// </summary>
    public SoftDeleteInterceptor(AppReadConnectionString connectionString, IHttpContextAccessor httpContextAccessor)
    {
        _connectionString = connectionString;
        _httpContextAccessor = httpContextAccessor;
        _tenantOverride = null;
    }

    /// <summary>
    /// Construtor de teste — recebe o tenant accessor direto. NÃO usar em DI de produção
    /// porque cria captive dependency sob <c>AddDbContextPool</c>.
    /// </summary>
    public SoftDeleteInterceptor(AppReadConnectionString connectionString, ICurrentTenantAccessor tenant)
    {
        _connectionString = connectionString;
        _httpContextAccessor = null;
        _tenantOverride = tenant;
    }

    private ICurrentTenantAccessor? ResolveTenant()
    {
        if (_tenantOverride is not null) return _tenantOverride;
        var httpCtx = _httpContextAccessor?.HttpContext;
        return httpCtx?.RequestServices.GetService<ICurrentTenantAccessor>();
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        InspectAndBlock(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await InspectAndBlockAsync(eventData.Context, cancellationToken);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void InspectAndBlock(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Deleted) continue;
            if (entry.Entity is not ISoftDeletable) continue;

            RegistrarTentativaSync(entry);
            throw new BusinessException(
                "Não é permitido excluir registros desta entidade. Use a operação de exclusão lógica.");
        }
    }

    private async Task InspectAndBlockAsync(DbContext? context, CancellationToken ct)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Deleted) continue;
            if (entry.Entity is not ISoftDeletable) continue;

            await RegistrarTentativaAsync(entry, ct);
            throw new BusinessException(
                "Não é permitido excluir registros desta entidade. Use a operação de exclusão lógica.");
        }
    }

    private void RegistrarTentativaSync(EntityEntry entry)
    {
        var (tabela, registroId, estabId) = ExtrairMetadados(entry);
        using var conn = new NpgsqlConnection(_connectionString.Value);
        conn.Open();
        conn.Execute(SqlInsert, MontarParametros(tabela, registroId, estabId));
    }

    private async Task RegistrarTentativaAsync(EntityEntry entry, CancellationToken ct)
    {
        var (tabela, registroId, estabId) = ExtrairMetadados(entry);
        await using var conn = new NpgsqlConnection(_connectionString.Value);
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            SqlInsert, MontarParametros(tabela, registroId, estabId), cancellationToken: ct));
    }

    private object MontarParametros(string tabela, string registroId, long? estabelecimentoId)
    {
        var tenant = ResolveTenant();
        Guid? usuarioId = tenant is not null && tenant.UsuarioId != Guid.Empty
            ? tenant.UsuarioId
            : null;

        return new
        {
            Tabela = tabela,
            RegistroId = registroId,
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = usuarioId,
            Motivo = "Tentativa de hard delete bloqueada pelo SoftDeleteInterceptor.",
            TentadoEm = DateTime.UtcNow
        };
    }

    private const string SqlInsert = """
        INSERT INTO public.audit_delete_attempts
            (tabela, registro_id, estabelecimento_id, usuario_id, motivo, tentado_em)
        VALUES
            (@Tabela, @RegistroId, @EstabelecimentoId, @UsuarioId, @Motivo, @TentadoEm)
        """;

    private static (string Tabela, string RegistroId, long? EstabelecimentoId) ExtrairMetadados(EntityEntry entry)
    {
        var tabela = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name;
        var pk = entry.Metadata.FindPrimaryKey();
        var registroId = pk is null
            ? "?"
            : string.Join(",", pk.Properties.Select(p => entry.OriginalValues[p]?.ToString() ?? "?"));

        long? estabId = null;
        var estabProp = entry.Entity.GetType().GetProperty("EstabelecimentoId",
            BindingFlags.Public | BindingFlags.Instance);
        if (estabProp is not null && estabProp.PropertyType == typeof(long))
        {
            estabId = (long?)estabProp.GetValue(entry.Entity);
        }

        return (tabela, registroId, estabId);
    }
}
