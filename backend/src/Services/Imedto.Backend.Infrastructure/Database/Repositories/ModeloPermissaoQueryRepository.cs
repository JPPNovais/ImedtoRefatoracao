using System.Text.Json;
using Dapper;
using Imedto.Backend.Contracts.ModelosPermissao.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ModeloPermissaoQueryRepository
{
    private readonly string _connStr;

    public ModeloPermissaoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IEnumerable<ModeloPermissaoDto>> ListarPorEstabelecimento(long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT
                id                  AS Id,
                estabelecimento_id  AS EstabelecimentoId,
                nome                AS Nome,
                tipo_acesso         AS TipoAcesso,
                permissoes::text    AS PermissoesJson,
                eh_padrao           AS EhPadrao,
                criado_em           AS CriadoEm,
                atualizado_em       AS AtualizadoEm
            FROM public.modelo_permissao_estabelecimento
            WHERE estabelecimento_id = @EstabelecimentoId
            ORDER BY eh_padrao DESC, nome
            """;

        var rows = await conn.QueryAsync<ModeloPermissaoLinha>(sql, new { EstabelecimentoId = estabelecimentoId });

        return rows.Select(r => new ModeloPermissaoDto
        {
            Id = r.Id,
            EstabelecimentoId = r.EstabelecimentoId,
            Nome = r.Nome,
            TipoAcesso = r.TipoAcesso,
            Permissoes = ParsePermissoes(r.PermissoesJson),
            EhPadrao = r.EhPadrao,
            CriadoEm = r.CriadoEm,
            AtualizadoEm = r.AtualizadoEm,
        });
    }

    /// <summary>
    /// O legado usou jsonb com objetos no início (<c>{}</c>) — entradas antigas podem
    /// vir nesse formato. Tratamos como vazio para não quebrar o serializer.
    /// </summary>
    private static IReadOnlyList<string> ParsePermissoes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}") return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private class ModeloPermissaoLinha
    {
        public long Id { get; set; }
        public long EstabelecimentoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string TipoAcesso { get; set; } = string.Empty;
        public string? PermissoesJson { get; set; }
        public bool EhPadrao { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
    }
}
