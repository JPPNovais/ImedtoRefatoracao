using Dapper;
using Imedto.Backend.Domain.Migracao;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Migracao;

/// <summary>
/// Implementação Dapper de <see cref="IMigracaoAgendamentoLookup"/>.
/// Resolve profissional por nome no tenant para carga de agendamentos históricos.
///
/// Singleton-safe: abre conexão por chamada (pool de NpgsqlConnection).
/// Sem PII nos resultados — retorna apenas Guid.
/// </summary>
public class DapperAgendamentoMigracaoLookup : IMigracaoAgendamentoLookup
{
    private readonly AppReadConnectionString _cs;

    public DapperAgendamentoMigracaoLookup(AppReadConnectionString cs) => _cs = cs;

    public async Task<Guid?> ObterProfissionalIdPorNomeOuNulo(
        string nome, long estabelecimentoId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_cs.Value);
        // Busca via vínculo ativo no tenant — garante que o profissional tem acesso ao estabelecimento.
        // LOWER/ILIKE — case-insensitive sem PII no log.
        // Retorna null se não encontrado ou se ambíguo (LIMIT 2 → mais de 1 resultado → null).
        var ids = await conn.QueryAsync<Guid>(
            @"SELECT v.profissional_usuario_id
              FROM vinculo_profissional_estabelecimento v
              JOIN usuarios u ON u.id = v.profissional_usuario_id
              WHERE v.estabelecimento_id = @EstId
                AND v.status <> 'Inativo'
                AND LOWER(u.nome_completo) = LOWER(@Nome)
              LIMIT 2",
            new { EstId = estabelecimentoId, Nome = nome });

        var list = ids.ToList();
        return list.Count == 1 ? list[0] : null;
    }
}
