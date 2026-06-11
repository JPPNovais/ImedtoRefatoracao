using Dapper;
using Npgsql;
using Imedto.Backend.Contracts.Admin.ConfigTrial.Queries;
using Imedto.Backend.Contracts.Admin.ConfigTrial.Queries.Results;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Domain.Admin;

namespace Imedto.Backend.Application.Admin.ConfigTrial;

public class ObterConfigTrialAdminQueryHandler
{
    private readonly string _connectionString;

    public ObterConfigTrialAdminQueryHandler(AppReadConnectionString conn)
    {
        _connectionString = conn.Value;
    }

    public async Task<ConfigTrialAdminDto?> Handle(ObterConfigTrialAdminQuery query, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                c.id                AS Id,
                c.plano_trial_id    AS PlanoTrialId,
                p.nome              AS PlanoTrialNome,
                c.duracao_trial_dias AS DuracaoTrialDias,
                c.trial_habilitado  AS TrialHabilitado,
                c.atualizado_em     AS AtualizadoEm
            FROM imedto_config_trial c
            INNER JOIN imedto_planos p ON p.id = c.plano_trial_id
            WHERE c.id = @Id
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<ConfigTrialAdminDto>(
            new CommandDefinition(sql, new { Id = ImedtoConfigTrial.IdFixo }, cancellationToken: ct));
    }
}
