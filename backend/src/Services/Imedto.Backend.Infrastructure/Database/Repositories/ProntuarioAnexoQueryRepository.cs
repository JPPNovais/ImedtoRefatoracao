using Dapper;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Tenancy;
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
    /// Lista paginada de anexos do prontuário, gated por autor-ou-dono (R7 briefing 2026-06-27_001).
    /// Predicado COALESCE de autoria:
    ///   - Anexo COM evolução → visível ao autor da evolução (pe.autor_usuario_id) OU Dono.
    ///   - Anexo ÓRFÃO (evolucao_id IS NULL) → visível a quem fez upload (criado_por_usuario_id) OU Dono.
    /// </summary>
    public virtual async Task<(IEnumerable<AnexoDto> Itens, int Total)> ListarDoProntuario(
        long prontuarioId,
        long? evolucaoId,
        Guid solicitanteUsuarioId,
        TenantPapel papel,
        int pagina = 1,
        int tamanho = 50)
    {
        pagina = Math.Max(pagina, 1);
        tamanho = Math.Clamp(tamanho, 1, 100);
        var offset = (pagina - 1) * tamanho;

        // Gating COALESCE de autoria (R7): usa autor_usuario_id da evolução quando há evolução;
        // usa criado_por_usuario_id do anexo quando órfão; Dono sempre passa (@Papel = 'Dono').
        const string gatingClause = """
            AND (
                @Papel = 'Dono'
                OR (a.evolucao_id IS NOT NULL AND pe.autor_usuario_id = @UsuarioId)
                OR (a.evolucao_id IS NULL AND a.criado_por_usuario_id = @UsuarioId)
            )
            """;

        var sqlCount = $"""
            SELECT COUNT(*)
            FROM   public.prontuario_anexos a
            LEFT JOIN public.prontuario_evolucoes pe ON pe.id = a.evolucao_id
            WHERE  a.prontuario_id = @ProntuarioId
              AND  a.arquivado_em IS NULL
              AND  a.deletado_em IS NULL
              AND  (@EvolucaoId IS NULL OR a.evolucao_id = @EvolucaoId)
              {gatingClause}
            """;

        var sqlItens = $"""
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
            LEFT JOIN public.prontuario_evolucoes pe ON pe.id = a.evolucao_id
            LEFT JOIN public.usuarios u ON u.id = a.criado_por_usuario_id
            WHERE   a.prontuario_id = @ProntuarioId
              AND   a.arquivado_em IS NULL
              AND   a.deletado_em IS NULL
              AND   (@EvolucaoId IS NULL OR a.evolucao_id = @EvolucaoId)
              {gatingClause}
            ORDER BY a.criado_em DESC
            LIMIT  @Tamanho OFFSET @Offset
            """;

        var parametros = new
        {
            ProntuarioId = prontuarioId,
            EvolucaoId = evolucaoId,
            UsuarioId = solicitanteUsuarioId,
            Papel = papel.ToString(),
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
    /// Gating por COALESCE de autoria (R7 briefing 2026-06-27_001).
    /// </summary>
    public virtual async Task<IEnumerable<(long AnexoId, long ProntuarioId, string StoragePath, string Nome, string Mime)>>
        ObterReferenciasAnexos(
            IReadOnlyList<long> anexoIds,
            long pacienteId,
            long estabelecimentoId,
            Guid solicitanteUsuarioId,
            TenantPapel papel)
    {
        if (anexoIds.Count == 0) return Enumerable.Empty<(long, long, string, string, string)>();

        const string sql = """
            SELECT a.id            AS AnexoId,
                   a.prontuario_id AS ProntuarioId,
                   a.storage_path  AS StoragePath,
                   a.nome_original AS Nome,
                   a.mime_type     AS Mime
            FROM   public.prontuario_anexos a
            JOIN   public.prontuarios p
                   ON p.id = a.prontuario_id
                  AND p.estabelecimento_id = a.estabelecimento_id
            LEFT JOIN public.prontuario_evolucoes pe ON pe.id = a.evolucao_id
            WHERE  a.id = ANY(@AnexoIds)
              AND  a.estabelecimento_id = @EstabelecimentoId
              AND  p.paciente_id = @PacienteId
              AND  a.arquivado_em IS NULL
              AND  a.deletado_em IS NULL
              AND (
                  @Papel = 'Dono'
                  OR (a.evolucao_id IS NOT NULL AND pe.autor_usuario_id = @UsuarioId)
                  OR (a.evolucao_id IS NULL AND a.criado_por_usuario_id = @UsuarioId)
              )
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync<AnexoReferenciaRow>(sql, new
        {
            AnexoIds = anexoIds.ToArray(),
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            UsuarioId = solicitanteUsuarioId,
            Papel = papel.ToString(),
        });

        return rows.Select(r => (r.AnexoId, r.ProntuarioId, r.StoragePath, r.Nome, r.Mime));
    }

    private sealed record AnexoReferenciaRow(long AnexoId, long ProntuarioId, string StoragePath, string Nome, string Mime);

    /// <summary>
    /// Carrega referencia do anexo com defense-in-depth LGPD (tenant + paciente)
    /// E gating por COALESCE de autoria (R7 briefing 2026-06-27_001).
    /// Retorna null quando o anexo não existe, pertence a outro tenant/paciente,
    /// ou o solicitante não é o autor/uploader (nem Dono) — mensagem genérica no handler (R5).
    /// </summary>
    public virtual async Task<(long ProntuarioId, string StoragePath, string Nome, string Mime)?>
        ObterReferenciaAnexo(
            long anexoId,
            long pacienteId,
            long estabelecimentoId,
            Guid solicitanteUsuarioId,
            TenantPapel papel)
    {
        const string sql = """
            SELECT a.prontuario_id, a.storage_path, a.nome_original, a.mime_type
            FROM   public.prontuario_anexos a
            JOIN   public.prontuarios p
                   ON p.id = a.prontuario_id
                  AND p.estabelecimento_id = a.estabelecimento_id
            LEFT JOIN public.prontuario_evolucoes pe ON pe.id = a.evolucao_id
            WHERE  a.id = @AnexoId
              AND  a.estabelecimento_id = @EstabelecimentoId
              AND  p.paciente_id = @PacienteId
              AND  a.arquivado_em IS NULL
              AND  a.deletado_em IS NULL
              AND (
                  @Papel = 'Dono'
                  OR (a.evolucao_id IS NOT NULL AND pe.autor_usuario_id = @UsuarioId)
                  OR (a.evolucao_id IS NULL AND a.criado_por_usuario_id = @UsuarioId)
              )
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<(long, string, string, string)?>(sql, new
        {
            AnexoId = anexoId,
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = solicitanteUsuarioId,
            Papel = papel.ToString(),
        });
    }
}
