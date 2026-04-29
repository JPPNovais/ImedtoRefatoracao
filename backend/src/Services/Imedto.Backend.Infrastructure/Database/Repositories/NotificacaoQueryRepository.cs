using Dapper;
using Imedto.Backend.Contracts.Notificacoes.Queries;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Leitura via Dapper. Sempre filtra por <c>usuario_id</c> — RLS é defense-in-depth,
/// mas o backend é a fonte da verdade do isolamento (ver CLAUDE.md, seção LGPD).
/// </summary>
public class NotificacaoQueryRepository
{
    private readonly string _connStr;

    public NotificacaoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<PaginaNotificacoesDto> Listar(Guid usuarioId, bool? lidas, int pagina, int tamanho)
    {
        if (pagina < 1) pagina = 1;
        if (tamanho < 1 || tamanho > 100) tamanho = 20;
        var offset = (pagina - 1) * tamanho;

        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT count(*) FROM notificacoes
             WHERE usuario_id = @UsuarioId
               AND (@Lidas::bool IS NULL OR lida = @Lidas::bool);

            SELECT
                id              AS Id,
                estabelecimento_id AS EstabelecimentoId,
                titulo          AS Titulo,
                mensagem        AS Mensagem,
                categoria       AS Categoria,
                link_acao       AS LinkAcao,
                lida            AS Lida,
                criada_em       AS CriadaEm,
                lida_em         AS LidaEm
            FROM notificacoes
            WHERE usuario_id = @UsuarioId
              AND (@Lidas::bool IS NULL OR lida = @Lidas::bool)
            ORDER BY criada_em DESC
            LIMIT @Tamanho OFFSET @Offset;
            """;

        await using var multi = await conn.QueryMultipleAsync(sql, new
        {
            UsuarioId = usuarioId,
            Lidas = lidas,
            Tamanho = tamanho,
            Offset = offset
        });

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<NotificacaoDto>();

        return new PaginaNotificacoesDto
        {
            Total = total,
            Pagina = pagina,
            Tamanho = tamanho,
            Itens = itens
        };
    }

    public async Task<int> ContarNaoLidas(Guid usuarioId)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM notificacoes WHERE usuario_id = @UsuarioId AND lida = false",
            new { UsuarioId = usuarioId });
    }
}
