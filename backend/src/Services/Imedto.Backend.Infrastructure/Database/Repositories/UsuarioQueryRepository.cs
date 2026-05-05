using Dapper;
using Imedto.Backend.Contracts.Auth.Queries.Results;
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

    /// <summary>
    /// Projeção de leitura usada pelo /auth/bootstrap. Retorna apenas os campos
    /// que o front precisa para reidratar a sessão (LGPD — minimização). Status
    /// volta como texto da enum (mesmo formato que o /auth/me devolve).
    /// </summary>
    public async Task<MeUsuarioDto?> ObterMeParaBootstrap(Guid usuarioId)
    {
        const string sql = """
            SELECT  id                  AS Id,
                    email               AS Email,
                    nome_completo       AS NomeCompleto,
                    telefone            AS Telefone,
                    status              AS Status,
                    onboarding_completo AS OnboardingCompleto
            FROM    public.usuarios
            WHERE   id = @Id
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<MeUsuarioDto>(sql, new { Id = usuarioId });
    }
}
