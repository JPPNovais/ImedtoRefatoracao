using Dapper;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ProntuarioAnexoQueryRepository
{
    private readonly string _connectionString;

    public ProntuarioAnexoQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    public async Task<IEnumerable<AnexoDto>> ListarDoProntuario(long prontuarioId, long? evolucaoId)
    {
        const string sql = """
            SELECT  a.id                AS Id,
                    a.prontuario_id     AS ProntuarioId,
                    a.evolucao_id       AS EvolucaoId,
                    a.nome_original     AS NomeOriginal,
                    a.mime_type         AS MimeType,
                    a.tamanho_bytes     AS TamanhoBytes,
                    a.criado_em         AS CriadoEm,
                    u.nome_completo     AS AutorNome
            FROM    public.prontuario_anexos a
            LEFT JOIN public.usuarios u ON u.id = a.criado_por_usuario_id
            WHERE   a.prontuario_id = @ProntuarioId
              AND   a.arquivado_em IS NULL
              AND   a.deletado_em IS NULL
              AND   (@EvolucaoId IS NULL OR a.evolucao_id = @EvolucaoId)
            ORDER BY a.criado_em DESC
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<AnexoDto>(sql, new
        {
            ProntuarioId = prontuarioId,
            EvolucaoId = evolucaoId
        });
    }

    public async Task<(long ProntuarioId, long EstabelecimentoId, string StoragePath, string Nome, string Mime)?>
        ObterReferenciaAnexo(long anexoId)
    {
        const string sql = """
            SELECT prontuario_id, estabelecimento_id, storage_path, nome_original, mime_type
            FROM   public.prontuario_anexos
            WHERE  id = @AnexoId
              AND  arquivado_em IS NULL
              AND  deletado_em IS NULL
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<(long, long, string, string, string)?>(sql, new { AnexoId = anexoId });
    }
}
