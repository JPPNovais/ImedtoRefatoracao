using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
/// </summary>
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly AppReadConnectionString _connectionString;
    private readonly ICurrentTenantAccessor _tenant;

    public SoftDeleteInterceptor(AppReadConnectionString connectionString, ICurrentTenantAccessor tenant)
    {
        _connectionString = connectionString;
        _tenant = tenant;
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

    private object MontarParametros(string tabela, string registroId, long? estabelecimentoId) => new
    {
        Tabela = tabela,
        RegistroId = registroId,
        EstabelecimentoId = estabelecimentoId,
        UsuarioId = _tenant.TemTenantDefinido && _tenant.UsuarioId != Guid.Empty
            ? (Guid?)_tenant.UsuarioId
            : null,
        Motivo = "Tentativa de hard delete bloqueada pelo SoftDeleteInterceptor.",
        TentadoEm = DateTime.UtcNow
    };

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
