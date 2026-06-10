using Dapper;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Abstração para resolver destinatários de alertas de estoque.
/// Separada para permitir mock em testes unitários do handler.
/// </summary>
public interface IInventarioNotificacaoQueryRepository
{
    Task<IReadOnlyList<Guid>> ListarUsuariosComAcaoEstoque(long estabelecimentoId);
}

/// <summary>
/// Repositório de leitura (Dapper, singleton) para resolver destinatários de alertas de estoque.
/// Usado exclusivamente pelo <c>EstoqueAbaixoMinimoEventHandler</c>.
/// </summary>
public class InventarioNotificacaoQueryRepository : IInventarioNotificacaoQueryRepository
{
    private readonly string _connStr;

    public InventarioNotificacaoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    /// <summary>
    /// Retorna os <c>usuario_id</c> distintos que possuem a ação <c>estoque</c> no estabelecimento:
    /// o dono (sempre) + profissionais com vínculo ativo cujo modelo de permissão concede
    /// qualquer ação da área <c>estoque</c>.
    ///
    /// Falha-fechada: se <paramref name="estabelecimentoId"/> for zero ou negativo, retorna lista vazia.
    /// Nenhum cruzamento de tenant: a query filtra <c>estabelecimento_id = @EstabId</c> em todas as tabelas.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> ListarUsuariosComAcaoEstoque(long estabelecimentoId)
    {
        // Falha-fechada — sem tenant claim válido, não faz query global.
        if (estabelecimentoId <= 0)
            return [];

        await using var conn = new NpgsqlConnection(_connStr);

        // União de dois conjuntos, ambos filtrados pelo mesmo estabelecimento_id:
        // 1) O dono do estabelecimento (sempre tem a ação estoque).
        // 2) Profissionais com vínculo Ativo cujo modelo concede área "estoque"
        //    (qualquer chave que seja "estoque" exatamente OU comece com "estoque.").
        //
        // A permissão é armazenada como jsonb array de strings em modelo_permissao_estabelecimento.permissoes.
        // Casos cobertos:
        //   - Chave legada  → "estoque"             (mp.permissoes @> '["estoque"]')
        //   - Chave granular→ "estoque.<acao>"       (EXISTS jsonb_array_elements LIKE 'estoque.%')
        const string sql = """
            SELECT e.dono_usuario_id AS UsuarioId
            FROM   public.estabelecimentos e
            WHERE  e.id = @EstabId

            UNION

            SELECT v.profissional_usuario_id AS UsuarioId
            FROM   public.vinculo_profissional_estabelecimento v
            JOIN   public.modelo_permissao_estabelecimento mp ON mp.id = v.modelo_permissao_id
            WHERE  v.estabelecimento_id = @EstabId
              AND  v.status = 'Ativo'
              AND  (
                    mp.permissoes @> '["estoque"]'::jsonb
                 OR EXISTS (
                        SELECT 1
                        FROM   jsonb_array_elements_text(mp.permissoes) AS p(val)
                        WHERE  p.val LIKE 'estoque.%'
                    )
              );
            """;

        var resultado = await conn.QueryAsync<Guid>(sql, new { EstabId = estabelecimentoId });
        return resultado.AsList();
    }
}
