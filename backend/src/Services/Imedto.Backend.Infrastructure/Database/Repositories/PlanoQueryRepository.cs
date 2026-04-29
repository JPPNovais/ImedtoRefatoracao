using System.Text.Json;
using Dapper;
using Imedto.Backend.Contracts.Assinaturas.Queries;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Leitura via Dapper para o catálogo de planos. <c>features_json</c> chega como string
/// e é desserializada no mapper — evita dependência do Npgsql JSONB type handler para listas.
/// </summary>
public class PlanoQueryRepository
{
    private readonly string _connStr;

    public PlanoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IEnumerable<PlanoDto>> ListarAtivos()
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                id                    AS Id,
                nome                  AS Nome,
                preco_mensal          AS PrecoMensal,
                limite_profissionais  AS LimiteProfissionais,
                limite_pacientes      AS LimitePacientes,
                features_json::text   AS FeaturesJson,
                ativo                 AS Ativo,
                ordem                 AS Ordem
            FROM planos
            WHERE ativo = true
            ORDER BY ordem ASC, preco_mensal ASC, nome ASC;
            """;

        var linhas = await conn.QueryAsync<PlanoQueryRow>(sql);
        return linhas.Select(MapearLinha).ToList();
    }

    public async Task<PlanoDto?> ObterPorId(long id)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                id                    AS Id,
                nome                  AS Nome,
                preco_mensal          AS PrecoMensal,
                limite_profissionais  AS LimiteProfissionais,
                limite_pacientes      AS LimitePacientes,
                features_json::text   AS FeaturesJson,
                ativo                 AS Ativo,
                ordem                 AS Ordem
            FROM planos
            WHERE id = @Id;
            """;

        var linha = await conn.QueryFirstOrDefaultAsync<PlanoQueryRow>(sql, new { Id = id });
        return linha is null ? null : MapearLinha(linha);
    }

    private static PlanoDto MapearLinha(PlanoQueryRow row)
    {
        return new PlanoDto
        {
            Id = row.Id,
            Nome = row.Nome,
            PrecoMensal = row.PrecoMensal,
            LimiteProfissionais = row.LimiteProfissionais,
            LimitePacientes = row.LimitePacientes,
            Features = ParseFeatures(row.FeaturesJson),
            Ativo = row.Ativo,
            Ordem = row.Ordem
        };
    }

    private static IEnumerable<string> ParseFeatures(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            // Defensivo: se o JSON estiver malformado por algum motivo, não quebra a tela.
            return Array.Empty<string>();
        }
    }

    /// <summary>Linha intermediária para mapear o jsonb como string antes de desserializar.</summary>
    private sealed class PlanoQueryRow
    {
        public long Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal PrecoMensal { get; set; }
        public int? LimiteProfissionais { get; set; }
        public int? LimitePacientes { get; set; }
        public string? FeaturesJson { get; set; }
        public bool Ativo { get; set; }
        public int Ordem { get; set; }
    }
}
