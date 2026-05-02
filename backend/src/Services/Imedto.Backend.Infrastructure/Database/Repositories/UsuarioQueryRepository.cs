using Dapper;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Read-side de Usuarios (Dapper). Usado por queries leves como
/// disponibilidade de CPF — operações que não justificam abrir o EF DbContext.
/// </summary>
public class UsuarioQueryRepository
{
    private readonly string _connectionString;

    public UsuarioQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    /// <summary>
    /// Verifica se o CPF (apenas dígitos) já existe em outro usuário.
    /// Ignora o próprio usuário corrente para permitir reidempotência ao reenviar onboarding.
    /// </summary>
    public async Task<bool> ExisteCpfEmOutroUsuario(string cpfDigitos, Guid ignorarUsuarioId)
    {
        if (string.IsNullOrEmpty(cpfDigitos)) return false;

        const string sql = """
            SELECT EXISTS(
                SELECT 1
                FROM   public.usuarios
                WHERE  cpf = @cpf
                  AND  id <> @ignorar
            )
        """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<bool>(sql, new { cpf = cpfDigitos, ignorar = ignorarUsuarioId });
    }
}
