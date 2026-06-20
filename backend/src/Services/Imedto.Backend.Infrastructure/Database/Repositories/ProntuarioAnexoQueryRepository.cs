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

    /// <summary>
    /// Lista paginada de anexos do prontuário.
    /// Retrocompatível: <paramref name="pagina"/>=1 e <paramref name="tamanho"/>=50 é o default
    /// anterior (sem paginação retornava tudo; agora retorna a 1ª página, que cobre a maioria dos casos).
    /// </summary>
    public virtual async Task<(IEnumerable<AnexoDto> Itens, int Total)> ListarDoProntuario(
        long prontuarioId,
        long? evolucaoId,
        int pagina = 1,
        int tamanho = 50)
    {
        pagina = Math.Max(pagina, 1);
        tamanho = Math.Clamp(tamanho, 1, 100);
        var offset = (pagina - 1) * tamanho;

        // SELECT minimizado (LGPD): sem prontuario_id (front nao usa).
        const string sqlCount = """
            SELECT COUNT(*)
            FROM   public.prontuario_anexos a
            WHERE  a.prontuario_id = @ProntuarioId
              AND  a.arquivado_em IS NULL
              AND  a.deletado_em IS NULL
              AND  (@EvolucaoId IS NULL OR a.evolucao_id = @EvolucaoId)
            """;

        const string sqlItens = """
            SELECT  a.id                AS Id,
                    a.evolucao_id       AS EvolucaoId,
                    a.nome_original     AS NomeOriginal,
                    a.mime_type         AS MimeType,
                    a.tamanho_bytes     AS TamanhoBytes,
                    a.criado_em         AS CriadoEm,
                    u.nome_completo     AS AutorNome,
                    a.regiao_anatomica  AS RegiaoAnatomica,
                    a.marcador          AS Marcador
            FROM    public.prontuario_anexos a
            LEFT JOIN public.usuarios u ON u.id = a.criado_por_usuario_id
            WHERE   a.prontuario_id = @ProntuarioId
              AND   a.arquivado_em IS NULL
              AND   a.deletado_em IS NULL
              AND   (@EvolucaoId IS NULL OR a.evolucao_id = @EvolucaoId)
            ORDER BY a.criado_em DESC
            LIMIT  @Tamanho OFFSET @Offset
            """;

        var parametros = new
        {
            ProntuarioId = prontuarioId,
            EvolucaoId = evolucaoId,
            Tamanho = tamanho,
            Offset = offset
        };

        // Duas queries na mesma conexão — sequencialmente (Npgsql não suporta paralelo na mesma conn).
        await using var conn = new NpgsqlConnection(_connectionString);
        var total = await conn.ExecuteScalarAsync<int>(sqlCount, parametros);
        var itens = await conn.QueryAsync<AnexoDto>(sqlItens, parametros);

        return (itens, total);
    }

    /// <summary>
    /// Retorna os caminhos de storage de múltiplos anexos de um mesmo paciente/estabelecimento.
    /// Defense-in-depth: JOIN com prontuários garante que todos os anexoIds pertencem ao
    /// mesmo paciente e tenant — anexoIds de outro paciente/tenant simplesmente não retornam.
    /// </summary>
    public virtual async Task<IEnumerable<(long AnexoId, long ProntuarioId, string StoragePath, string Nome, string Mime)>>
        ObterReferenciasAnexos(IReadOnlyList<long> anexoIds, long pacienteId, long estabelecimentoId)
    {
        if (anexoIds.Count == 0) return Enumerable.Empty<(long, long, string, string, string)>();

        const string sql = """
            SELECT a.id AS AnexoId, a.prontuario_id AS ProntuarioId,
                   a.storage_path AS StoragePath, a.nome_original AS Nome, a.mime_type AS Mime
            FROM   public.prontuario_anexos a
            JOIN   public.prontuarios p
                   ON p.id = a.prontuario_id
                  AND p.estabelecimento_id = a.estabelecimento_id
            WHERE  a.id = ANY(@AnexoIds)
              AND  a.estabelecimento_id = @EstabelecimentoId
              AND  p.paciente_id = @PacienteId
              AND  a.arquivado_em IS NULL
              AND  a.deletado_em IS NULL
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync(sql, new
        {
            AnexoIds = anexoIds.ToArray(),
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId
        });

        return rows.Select(r => ((long)r.AnexoId, (long)r.ProntuarioId, (string)r.StoragePath, (string)r.Nome, (string)r.Mime));
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
