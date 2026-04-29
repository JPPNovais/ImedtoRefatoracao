using Dapper;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Read repository (Dapper) para solicitações de vínculo. Joins com usuários e estabelecimentos
/// trazem o mínimo necessário para a UX (nome fantasia, e-mail/nome do profissional).
/// LGPD: campos retornados são restritos ao que cada lista efetivamente exibe.
/// </summary>
public class SolicitacaoVinculoQueryRepository
{
    private readonly string _connectionString;

    public SolicitacaoVinculoQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    /// <summary>Solicitações enviadas pelo profissional (todos os status, mais recentes primeiro).</summary>
    public async Task<IEnumerable<SolicitacaoVinculoDto>> ListarPorProfissional(Guid profissionalUsuarioId)
    {
        const string sql = """
            SELECT  s.id                          AS Id,
                    s.profissional_usuario_id     AS ProfissionalUsuarioId,
                    u.email                       AS ProfissionalEmail,
                    u.nome_completo               AS ProfissionalNome,
                    s.estabelecimento_id          AS EstabelecimentoId,
                    e.nome_fantasia               AS EstabelecimentoNomeFantasia,
                    s.status                      AS Status,
                    s.mensagem                    AS Mensagem,
                    s.criada_em                   AS CriadaEm,
                    s.respondida_em               AS RespondidaEm,
                    s.motivo_recusa               AS MotivoRecusa
            FROM    public.solicitacoes_vinculo s
            JOIN    public.estabelecimentos e ON e.id = s.estabelecimento_id
            JOIN    public.usuarios u         ON u.id = s.profissional_usuario_id
            WHERE   s.profissional_usuario_id = @ProfissionalUsuarioId
            ORDER BY s.criada_em DESC
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<SolicitacaoVinculoDto>(sql, new { ProfissionalUsuarioId = profissionalUsuarioId });
    }

    /// <summary>
    /// Solicitações recebidas pelo estabelecimento. Filtra por status quando informado;
    /// caso contrário retorna todas (mais recentes primeiro).
    /// </summary>
    public async Task<IEnumerable<SolicitacaoVinculoDto>> ListarPorEstabelecimento(
        long estabelecimentoId, string statusFiltro)
    {
        const string sql = """
            SELECT  s.id                          AS Id,
                    s.profissional_usuario_id     AS ProfissionalUsuarioId,
                    u.email                       AS ProfissionalEmail,
                    u.nome_completo               AS ProfissionalNome,
                    s.estabelecimento_id          AS EstabelecimentoId,
                    e.nome_fantasia               AS EstabelecimentoNomeFantasia,
                    s.status                      AS Status,
                    s.mensagem                    AS Mensagem,
                    s.criada_em                   AS CriadaEm,
                    s.respondida_em               AS RespondidaEm,
                    s.motivo_recusa               AS MotivoRecusa
            FROM    public.solicitacoes_vinculo s
            JOIN    public.estabelecimentos e ON e.id = s.estabelecimento_id
            JOIN    public.usuarios u         ON u.id = s.profissional_usuario_id
            WHERE   s.estabelecimento_id = @EstabelecimentoId
              AND   (@Status IS NULL OR s.status = @Status)
            ORDER BY s.criada_em DESC
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<SolicitacaoVinculoDto>(
            sql,
            new { EstabelecimentoId = estabelecimentoId, Status = statusFiltro });
    }
}
