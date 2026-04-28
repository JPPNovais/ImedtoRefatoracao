using Dapper;
using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class OrcamentoQueryRepository
{
    private readonly string _connStr;

    public OrcamentoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IEnumerable<OrcamentoResumoDto>> Listar(
        long estabelecimentoId,
        long? pacienteId,
        string? status)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                o.id                    AS Id,
                o.estabelecimento_id    AS EstabelecimentoId,
                o.paciente_id           AS PacienteId,
                pac.nome_completo       AS PacienteNome,
                o.numero                AS Numero,
                o.status                AS Status,
                o.validade              AS Validade,
                COALESCE(SUM(i.subtotal), 0) AS Total,
                COALESCE(u.nome_completo, u.email) AS CriadoPorNome,
                o.criado_em             AS CriadoEm,
                o.atualizado_em         AS AtualizadoEm
            FROM orcamentos o
            JOIN pacientes pac ON pac.id = o.paciente_id
            JOIN usuarios   u   ON u.id  = o.criado_por_usuario_id
            LEFT JOIN itens_orcamento i ON i.orcamento_id = o.id
            WHERE o.estabelecimento_id = @EstabelecimentoId
              AND (@PacienteId::bigint IS NULL OR o.paciente_id = @PacienteId::bigint)
              AND (@Status::text      IS NULL OR o.status = @Status::text)
            GROUP BY o.id, pac.nome_completo, u.nome_completo, u.email
            ORDER BY o.criado_em DESC
            """;

        return await conn.QueryAsync<OrcamentoResumoDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            Status = status
        });
    }

    public async Task<OrcamentoDto?> ObterPorId(long id)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sqlOrc = """
            SELECT
                o.id                    AS Id,
                o.estabelecimento_id    AS EstabelecimentoId,
                o.paciente_id           AS PacienteId,
                pac.nome_completo       AS PacienteNome,
                o.numero                AS Numero,
                o.status                AS Status,
                o.validade              AS Validade,
                o.observacoes           AS Observacoes,
                COALESCE(u.nome_completo, u.email) AS CriadoPorNome,
                o.criado_em             AS CriadoEm,
                o.atualizado_em         AS AtualizadoEm
            FROM orcamentos o
            JOIN pacientes pac ON pac.id = o.paciente_id
            JOIN usuarios   u   ON u.id  = o.criado_por_usuario_id
            WHERE o.id = @Id
            """;

        const string sqlItens = """
            SELECT
                id              AS Id,
                descricao       AS Descricao,
                quantidade      AS Quantidade,
                valor_unitario  AS ValorUnitario,
                desconto_percent AS DescontoPercent,
                subtotal        AS Subtotal
            FROM itens_orcamento
            WHERE orcamento_id = @Id
            ORDER BY id
            """;

        var orc = await conn.QuerySingleOrDefaultAsync<OrcamentoDto>(sqlOrc, new { Id = id });
        if (orc is null) return null;

        var itens = await conn.QueryAsync<ItemOrcamentoDto>(sqlItens, new { Id = id });
        orc.Itens = itens.ToList();
        orc.Total = orc.Itens.Sum(i => i.Subtotal);

        return orc;
    }
}
