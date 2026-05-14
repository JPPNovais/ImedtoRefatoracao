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
        // SELECT minimizado (LGPD): sem prontuario_id (front nao usa).
        const string sql = """
            SELECT  a.id                AS Id,
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

    /// <summary>
    /// Carrega referencia do anexo filtrando por <paramref name="estabelecimentoId"/>
    /// E <paramref name="pacienteId"/> dentro do SQL — defense-in-depth LGPD:
    /// anexo de outro tenant OU de outro paciente do mesmo tenant retorna null.
    ///
    /// O filtro por paciente fecha um vetor IDOR: antes, qualquer membro
    /// autenticado conseguia baixar anexo de qualquer paciente da clínica
    /// trocando apenas o <c>anexoId</c> na URL. Agora a URL exige o par
    /// (paciente, anexo) consistente com o que o banco persistiu — via JOIN
    /// com prontuários para validar a pertença do anexo ao paciente da rota.
    /// </summary>
    // virtual: permite Moq nos handlers (unit tests sem Postgres).
    public virtual async Task<(long ProntuarioId, string StoragePath, string Nome, string Mime)?>
        ObterReferenciaAnexo(long anexoId, long pacienteId, long estabelecimentoId)
    {
        const string sql = """
            SELECT a.prontuario_id, a.storage_path, a.nome_original, a.mime_type
            FROM   public.prontuario_anexos a
            JOIN   public.prontuarios p
                   ON p.id = a.prontuario_id
                  AND p.estabelecimento_id = a.estabelecimento_id
            WHERE  a.id = @AnexoId
              AND  a.estabelecimento_id = @EstabelecimentoId
              AND  p.paciente_id = @PacienteId
              AND  a.arquivado_em IS NULL
              AND  a.deletado_em IS NULL
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<(long, string, string, string)?>(sql, new
        {
            AnexoId = anexoId,
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId
        });
    }
}
