using Dapper;
using Imedto.Backend.Contracts.Termos.Dtos;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public interface ITermoEmitidoQueryRepository
{
    Task<IReadOnlyList<TermoEmitidoResumoDto>> ListarDoPaciente(long pacienteId, long estabelecimentoId, string status);
    Task<TermoEmitidoDetalheDto> ObterPorIdComSnapshot(long termoEmitidoId, long estabelecimentoId);
    Task<IReadOnlyList<TermoEmitidoResumoDto>> ListarDaEvolucao(long evolucaoId, long estabelecimentoId);
}

public sealed class TermoEmitidoQueryRepository : ITermoEmitidoQueryRepository
{
    private readonly string _connStr;

    public TermoEmitidoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IReadOnlyList<TermoEmitidoResumoDto>> ListarDoPaciente(long pacienteId, long estabelecimentoId, string status)
    {
        const string sql = """
            SELECT  t.id                AS Id,
                    t.paciente_id       AS PacienteId,
                    t.estabelecimento_id AS EstabelecimentoId,
                    t.termo_modelo_id   AS TermoModeloId,
                    m.titulo            AS TermoModeloTitulo,
                    m.categoria         AS Categoria,
                    t.versao_modelo     AS VersaoModelo,
                    t.status            AS Status,
                    t.assinatura_tipo   AS AssinaturaTipo,
                    t.assinado_em       AS AssinadoEm,
                    t.token_expira_em   AS TokenExpiraEm,
                    (t.pdf_url IS NOT NULL) AS TemPdf,
                    t.criado_em         AS CriadoEm,
                    t.evolucao_id       AS EvolucaoId,
                    t.emitido_por_usuario_id AS EmitidoPorUsuarioId,
                    u.nome_completo     AS EmitidoPorNome
            FROM    public.termo_emitido t
            LEFT JOIN public.termo_modelo m ON m.id = t.termo_modelo_id
            LEFT JOIN public.usuarios u ON u.id = t.emitido_por_usuario_id
            WHERE   t.paciente_id = @PacienteId
              AND   t.estabelecimento_id = @EstabelecimentoId
              AND   (@Status::text IS NULL OR t.status = @Status)
            ORDER BY t.criado_em DESC
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        var itens = await conn.QueryAsync<TermoEmitidoResumoDto>(sql, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
        });
        return itens.ToList();
    }

    public async Task<TermoEmitidoDetalheDto> ObterPorIdComSnapshot(long termoEmitidoId, long estabelecimentoId)
    {
        const string sql = """
            SELECT  t.id                AS Id,
                    t.paciente_id       AS PacienteId,
                    t.estabelecimento_id AS EstabelecimentoId,
                    t.termo_modelo_id   AS TermoModeloId,
                    m.titulo            AS TermoModeloTitulo,
                    m.categoria         AS Categoria,
                    t.versao_modelo     AS VersaoModelo,
                    t.status            AS Status,
                    t.assinatura_tipo   AS AssinaturaTipo,
                    t.assinado_em       AS AssinadoEm,
                    t.token_expira_em   AS TokenExpiraEm,
                    (t.pdf_url IS NOT NULL) AS TemPdf,
                    t.criado_em         AS CriadoEm,
                    t.evolucao_id       AS EvolucaoId,
                    t.emitido_por_usuario_id AS EmitidoPorUsuarioId,
                    u.nome_completo     AS EmitidoPorNome,
                    t.conteudo_snapshot_html  AS ConteudoSnapshotHtml,
                    t.conteudo_snapshot_texto AS ConteudoSnapshotTexto,
                    t.hash_integridade  AS HashIntegridade,
                    t.ip_assinatura     AS IpAssinatura,
                    t.user_agent_assinatura AS UserAgentAssinatura,
                    t.revogado_em       AS RevogadoEm,
                    t.revogado_motivo   AS RevogadoMotivo
            FROM    public.termo_emitido t
            LEFT JOIN public.termo_modelo m ON m.id = t.termo_modelo_id
            LEFT JOIN public.usuarios u ON u.id = t.emitido_por_usuario_id
            WHERE   t.id = @TermoEmitidoId
              AND   t.estabelecimento_id = @EstabelecimentoId
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QuerySingleOrDefaultAsync<TermoEmitidoDetalheDto>(sql, new
        {
            TermoEmitidoId = termoEmitidoId,
            EstabelecimentoId = estabelecimentoId,
        });
    }

    /// <summary>
    /// Lista termos vinculados a uma evolução específica — usado pela timeline da evolução (CA-C2).
    /// Multi-tenant: filtra por estabelecimento_id para garantir isolamento.
    /// </summary>
    public async Task<IReadOnlyList<TermoEmitidoResumoDto>> ListarDaEvolucao(long evolucaoId, long estabelecimentoId)
    {
        const string sql = """
            SELECT  t.id                AS Id,
                    t.paciente_id       AS PacienteId,
                    t.estabelecimento_id AS EstabelecimentoId,
                    t.termo_modelo_id   AS TermoModeloId,
                    m.titulo            AS TermoModeloTitulo,
                    m.categoria         AS Categoria,
                    t.versao_modelo     AS VersaoModelo,
                    t.status            AS Status,
                    t.assinatura_tipo   AS AssinaturaTipo,
                    t.assinado_em       AS AssinadoEm,
                    t.token_expira_em   AS TokenExpiraEm,
                    (t.pdf_url IS NOT NULL) AS TemPdf,
                    t.criado_em         AS CriadoEm,
                    t.evolucao_id       AS EvolucaoId,
                    t.emitido_por_usuario_id AS EmitidoPorUsuarioId,
                    u.nome_completo     AS EmitidoPorNome
            FROM    public.termo_emitido t
            LEFT JOIN public.termo_modelo m ON m.id = t.termo_modelo_id
            LEFT JOIN public.usuarios u ON u.id = t.emitido_por_usuario_id
            WHERE   t.evolucao_id = @EvolucaoId
              AND   t.estabelecimento_id = @EstabelecimentoId
            ORDER BY t.criado_em DESC
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        var itens = await conn.QueryAsync<TermoEmitidoResumoDto>(sql, new
        {
            EvolucaoId = evolucaoId,
            EstabelecimentoId = estabelecimentoId,
        });
        return itens.ToList();
    }
}
