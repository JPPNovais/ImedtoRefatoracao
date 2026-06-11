using System.Text.Json;
using Dapper;
using Imedto.Backend.Contracts.Assinaturas.Queries;
using Imedto.Backend.Domain.Assinaturas;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Opções compartilhadas para desserialização JSON — evita alocação por chamada (CA1869).
/// </summary>
file static class JsonOpts
{
    internal static readonly JsonSerializerOptions CaseInsensitive =
        new() { PropertyNameCaseInsensitive = true };
}

/// <summary>
/// Leitura de "minha assinatura" a partir da estrutura nova (imedto_assinaturas + imedto_planos).
/// Substitui o <see cref="AssinaturaQueryRepository"/> legado para o endpoint
/// <c>GET /api/minha-assinatura</c>, alinhando a fonte do front com o enforcement (F3).
///
/// Estado derivado usa a mesma lógica de
/// <see cref="Imedto.Backend.Domain.Admin.ImedtoAssinatura.ObterEstado"/>, expressa em SQL
/// para evitar N+1 e manter coerência sem carregar o aggregate inteiro via EF.
/// </summary>
public class MinhaAssinaturaQueryRepository
{
    private readonly string _connStr;

    public MinhaAssinaturaQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    /// <summary>
    /// Retorna a assinatura vigente do estabelecimento ou null se não há vigência.
    /// Multi-tenant: filtra exclusivamente por <paramref name="estabelecimentoId"/>;
    /// falha-fechada se o id for inválido.
    /// </summary>
    public virtual async Task<AssinaturaDto?> ObterDoEstabelecimento(long estabelecimentoId)
    {
        if (estabelecimentoId <= 0) return null;

        await using var conn = new NpgsqlConnection(_connStr);

        // Estado derivado em SQL espelha ImedtoAssinatura.ObterEstado():
        //   sem vigência (fim_em IS NOT NULL ou sem linha)  → tratado como ausência de resultado
        //   suspensa_em preenchido                          → Suspensa
        //   expira_em no passado                            → Expirada
        //   expira_em NULL                                  → Vitalicia (→ "Ativa" no DTO do front)
        //   expira_em no futuro                             → Temporaria (→ "Trial" no DTO do front)
        const string sql = """
            SELECT
                a.iniciada_em          AS IniciadaEm,
                a.expira_em            AS ExpiraEm,
                a.suspensa_em          AS SuspensaEm,
                p.nome                 AS PlanoNome,
                p.limites_json::text   AS LimitesJson,
                p.features_json::text  AS FeaturesJson
            FROM imedto_assinaturas a
            INNER JOIN imedto_planos p ON p.id = a.plano_id
            WHERE a.estabelecimento_id = @EstabelecimentoId
              AND a.fim_em IS NULL
            LIMIT 1;
            """;

        var linha = await conn.QueryFirstOrDefaultAsync<VigenciaRow>(sql,
            new { EstabelecimentoId = estabelecimentoId });

        if (linha is null) return null;

        var status = DerivarStatus(linha.SuspensaEm, linha.ExpiraEm);
        var features = ParsearFeatures(linha.FeaturesJson);
        var limites = ParsearLimites(linha.LimitesJson);

        return new AssinaturaDto
        {
            Plano = new PlanoDto
            {
                // Id não é consumido pelo front via este endpoint (assinaturaService mapeia
                // para MinhaAssinatura sem id numérico); 0 é inócuo.
                Id = 0,
                Nome = linha.PlanoNome,
                Features = features,
                LimiteProfissionais = limites.Profissionais,
                LimitePacientes = limites.Pacientes,
            },
            Status = status,
            IniciadaEm = linha.IniciadaEm.UtcDateTime,
            ExpiraEm = linha.ExpiraEm?.UtcDateTime,
            DiasRestantes = CalcularDiasRestantes(status, linha.ExpiraEm),
        };
    }

    /// <summary>
    /// Mapeia o estado da vigência para o vocabulário do DTO/front:
    /// Vitalícia → "Ativa", Temporária → "Trial", Suspensa → "Suspensa", Expirada → "Expirada".
    /// Mesma lógica de ImedtoAssinatura.ObterEstado() + coerência com isBlocked do front.
    /// </summary>
    private static string DerivarStatus(DateTimeOffset? suspensaEm, DateTimeOffset? expiraEm)
    {
        if (suspensaEm is not null) return "Suspensa";
        if (expiraEm.HasValue && expiraEm.Value <= DateTimeOffset.UtcNow) return "Expirada";
        if (expiraEm is null) return "Ativa";      // vitalício
        return "Trial";                             // expira_em no futuro
    }

    /// <summary>Dias restantes só fazem sentido para Trial (expira_em no futuro).</summary>
    private static int? CalcularDiasRestantes(string status, DateTimeOffset? expiraEm)
    {
        if (status != "Trial" || !expiraEm.HasValue) return null;
        var diff = expiraEm.Value - DateTimeOffset.UtcNow;
        if (diff <= TimeSpan.Zero) return 0;
        return (int)Math.Ceiling(diff.TotalDays);
    }

    /// <summary>
    /// Features: imedto_planos.features_json é {"receitas": true, "ia": false, ...}.
    /// Retorna apenas as chaves com valor true — mesmo formato que o AssinaturaService usa
    /// para ImedtoPlano.TemFeature.
    /// </summary>
    private static IEnumerable<string> ParsearFeatures(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}") return Array.Empty<string>();
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, bool>>(json,
                JsonOpts.CaseInsensitive);
            if (dict is null) return Array.Empty<string>();
            return dict.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static (int? Profissionais, int? Pacientes) ParsearLimites(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}") return (null, null);
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json,
                JsonOpts.CaseInsensitive);
            if (dict is null) return (null, null);

            int? ObterLimite(string chave)
            {
                foreach (var kv in dict)
                {
                    if (!string.Equals(kv.Key, chave, StringComparison.OrdinalIgnoreCase)) continue;
                    if (kv.Value.ValueKind == JsonValueKind.Null) return null;
                    if (kv.Value.TryGetInt32(out var v)) return v;
                    return null;
                }
                return null;
            }

            return (ObterLimite("profissionais"), ObterLimite("pacientes"));
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private sealed class VigenciaRow
    {
        public DateTimeOffset IniciadaEm { get; set; }
        public DateTimeOffset? ExpiraEm { get; set; }
        public DateTimeOffset? SuspensaEm { get; set; }
        public string PlanoNome { get; set; } = string.Empty;
        public string? LimitesJson { get; set; }
        public string? FeaturesJson { get; set; }
    }
}
