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
        // SELECT minimizado (LGPD): estabelecimento_id e atualizado_em removidos
        // — front nao consome (so estavam na interface TS sem uso real).
        const string sql = """
            SELECT
                id                  AS Id,
                nome                AS Nome,
                tipo_acesso         AS TipoAcesso,
                permissoes::text    AS PermissoesJson,
                eh_padrao           AS EhPadrao,
                criado_em           AS CriadoEm,
                icone               AS Icone,
                cor                 AS Cor,
                descricao           AS Descricao
            FROM public.modelo_permissao_estabelecimento
            WHERE estabelecimento_id = @EstabelecimentoId
            ORDER BY eh_padrao DESC, nome
            """;

        var rows = await conn.QueryAsync<ModeloPermissaoLinha>(sql, new { EstabelecimentoId = estabelecimentoId });

        return rows.Select(r => new ModeloPermissaoDto
        {
            Id = r.Id,
            Nome = r.Nome,
            TipoAcesso = r.TipoAcesso,
            Permissoes = ParsePermissoes(r.PermissoesJson),
            EhPadrao = r.EhPadrao,
            CriadoEm = r.CriadoEm,
            Icone = r.Icone,
            Cor = r.Cor,
            Descricao = r.Descricao,
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
        public string Nome { get; set; } = string.Empty;
        public string TipoAcesso { get; set; } = string.Empty;
        public string? PermissoesJson { get; set; }
        public bool EhPadrao { get; set; }
        public DateTime CriadoEm { get; set; }
        public string? Icone { get; set; }
        public string? Cor { get; set; }
        public string? Descricao { get; set; }
    }
}
