using System.Text.Json;
using Dapper;
using Imedto.Backend.Contracts.PedidosExame.Queries.Results;
using Imedto.Backend.SharedKernel.Tenancy;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public interface IPedidoExameQueryRepository
{
    Task<PaginaPedidosExameDto> ListarDoPaciente(
        long pacienteId,
        long estabelecimentoId,
        int pagina,
        int tamanhoPagina,
        Guid solicitanteUsuarioId,
        TenantPapel solicitantePapel);
    Task<PedidoExameDto?> ObterPorId(
        long pedidoExameId,
        long estabelecimentoId,
        Guid solicitanteUsuarioId,
        TenantPapel solicitantePapel);
}

/// <summary>
/// Read-side de pedidos de exame (Dapper). Sempre filtra por
/// <c>estabelecimento_id</c> e <c>deletado_em IS NULL</c>. Lista de exames vem
/// como string jsonb e é desserializada para <see cref="List{T}"/> aqui.
/// </summary>
public class PedidoExameQueryRepository : IPedidoExameQueryRepository
{
    private readonly string _connStr;

    public PedidoExameQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    /// <summary>Linha do banco — exames vem como string JSON; mapeia para DTO. </summary>
    private sealed class PedidoExameRaw
    {
        public long Id { get; set; }
        public long PacienteId { get; set; }
        public Guid ProfissionalUsuarioId { get; set; }
        public string? ProfissionalNome { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string? ExamesJson { get; set; }
        public string IndicacaoClinica { get; set; } = string.Empty;
        public string? Cid10 { get; set; }
        public string? Observacoes { get; set; }
        public DateTime CriadoEm { get; set; }

        public PedidoExameDto ToDto() => new()
        {
            Id = Id,
            PacienteId = PacienteId,
            ProfissionalUsuarioId = ProfissionalUsuarioId,
            ProfissionalNome = ProfissionalNome,
            Tipo = Tipo,
            Exames = string.IsNullOrEmpty(ExamesJson)
                ? Array.Empty<string>()
                : JsonSerializer.Deserialize<List<string>>(ExamesJson) ?? new List<string>(),
            IndicacaoClinica = IndicacaoClinica,
            Cid10 = Cid10,
            Observacoes = Observacoes,
            CriadoEm = CriadoEm,
        };
    }

    public async Task<PaginaPedidosExameDto> ListarDoPaciente(
        long pacienteId,
        long estabelecimentoId,
        int pagina,
        int tamanhoPagina,
        Guid solicitanteUsuarioId,
        TenantPapel solicitantePapel)
    {
        // Gating: @Papel = 'Dono' bypassa; profissional vê só os próprios (R1 briefing 2026-06-27_001).
        const string sqlTotal = """
            SELECT COUNT(*)
            FROM   public.pedidos_exame p
            WHERE  p.paciente_id = @PacienteId
              AND  p.estabelecimento_id = @EstabelecimentoId
              AND  p.deletado_em IS NULL
              AND  (@Papel = 'Dono' OR p.profissional_usuario_id = @UsuarioId)
            """;

        const string sqlItens = """
            SELECT  p.id                  AS Id,
                    p.paciente_id         AS PacienteId,
                    p.profissional_usuario_id AS ProfissionalUsuarioId,
                    u.nome_completo       AS ProfissionalNome,
                    p.tipo                AS Tipo,
                    p.exames::text        AS ExamesJson,
                    p.indicacao_clinica   AS IndicacaoClinica,
                    p.cid10               AS Cid10,
                    p.observacoes         AS Observacoes,
                    p.criado_em           AS CriadoEm
            FROM    public.pedidos_exame p
            LEFT JOIN public.usuarios u ON u.id = p.profissional_usuario_id
            WHERE   p.paciente_id = @PacienteId
              AND   p.estabelecimento_id = @EstabelecimentoId
              AND   p.deletado_em IS NULL
              AND   (@Papel = 'Dono' OR p.profissional_usuario_id = @UsuarioId)
            ORDER BY p.criado_em DESC
            LIMIT   @Tamanho OFFSET @Offset
            """;

        var parametros = new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = solicitanteUsuarioId,
            Papel = solicitantePapel.ToString(),
        };

        await using var conn = new NpgsqlConnection(_connStr);
        var total = await conn.ExecuteScalarAsync<int>(sqlTotal, parametros);

        var rows = await conn.QueryAsync<PedidoExameRaw>(sqlItens, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = solicitanteUsuarioId,
            Papel = solicitantePapel.ToString(),
            Tamanho = tamanhoPagina,
            Offset = (pagina - 1) * tamanhoPagina,
        });

        return new PaginaPedidosExameDto
        {
            Itens = rows.Select(r => r.ToDto()).ToList(),
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
        };
    }

    public async Task<PedidoExameDto?> ObterPorId(
        long pedidoExameId,
        long estabelecimentoId,
        Guid solicitanteUsuarioId,
        TenantPapel solicitantePapel)
    {
        // Gating: retorna null se o pedido não é do solicitante (e não é Dono) — mensagem genérica no handler (R5).
        const string sql = """
            SELECT  p.id                  AS Id,
                    p.paciente_id         AS PacienteId,
                    p.profissional_usuario_id AS ProfissionalUsuarioId,
                    u.nome_completo       AS ProfissionalNome,
                    p.tipo                AS Tipo,
                    p.exames::text        AS ExamesJson,
                    p.indicacao_clinica   AS IndicacaoClinica,
                    p.cid10               AS Cid10,
                    p.observacoes         AS Observacoes,
                    p.criado_em           AS CriadoEm
            FROM    public.pedidos_exame p
            LEFT JOIN public.usuarios u ON u.id = p.profissional_usuario_id
            WHERE   p.id = @Id
              AND   p.estabelecimento_id = @EstabelecimentoId
              AND   p.deletado_em IS NULL
              AND   (@Papel = 'Dono' OR p.profissional_usuario_id = @UsuarioId)
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        var row = await conn.QuerySingleOrDefaultAsync<PedidoExameRaw>(sql, new
        {
            Id = pedidoExameId,
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = solicitanteUsuarioId,
            Papel = solicitantePapel.ToString(),
        });
        return row?.ToDto();
    }
}
