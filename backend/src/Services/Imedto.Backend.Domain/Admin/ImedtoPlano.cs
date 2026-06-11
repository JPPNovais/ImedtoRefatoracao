using System.Text.Json;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Catálogo de planos gerenciado pelo admin global. Tabela global (sem estabelecimento_id).
///
/// ATENÇÃO: entidade separada de <c>Imedto.Backend.Domain.Assinaturas.Plano</c> que usa
/// IDs bigint e schema legado. Esta entidade usa UUID e campos estendidos para o módulo admin.
/// Nome da tabela no Postgres: <c>imedto_planos</c>.
/// </summary>
public class ImedtoPlano : Entity<Guid>
{
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual string? DescricaoCurta { get; protected set; }

    /// <summary>Preço mensal em centavos (nullable = preço sob consulta / customizado).</summary>
    public virtual int? PrecoMensalCentavos { get; protected set; }

    /// <summary>Flag de catálogo — true = plano sem cobrança (ex: Gratuidade Vitalícia).</summary>
    public virtual bool Gratuito { get; protected set; }

    /// <summary>Plano inativo não pode ser atribuído a novas assinaturas.</summary>
    public virtual bool Ativo { get; protected set; }

    /// <summary>Limites em JSON (ex: {"profissionais":5,"pacientes":100}). NULL/ausente = ilimitado.</summary>
    public virtual string LimitesJson { get; protected set; } = "{}";

    /// <summary>
    /// Features habilitadas neste plano (R5). JSON com as 8 chaves booleanas:
    /// receitas, exame_fisico, procedimentos_cirurgicos, orcamento_completo,
    /// ia, relatorios_avancados, automacoes_ilimitadas, anexos_ilimitados.
    /// Default '{}' = todas desabilitadas (exceto Gratuidade Vitalícia que semente com todas true).
    /// </summary>
    public virtual string FeaturesJson { get; protected set; } = "{}";

    public virtual DateTimeOffset CriadoEm { get; protected set; }
    public virtual DateTimeOffset? AtualizadoEm { get; protected set; }
    public virtual Guid? CriadoPorAdminId { get; protected set; }

    protected ImedtoPlano() { }

    public static ImedtoPlano Criar(
        string nome,
        string? descricaoCurta,
        int? precoMensalCentavos,
        bool gratuito,
        string limitesJson,
        Guid? criadoPorAdminId,
        string featuresJson = "{}")
    {
        ValidarNome(nome);
        if (precoMensalCentavos.HasValue && precoMensalCentavos.Value < 0)
            throw new BusinessException("Preço mensal não pode ser negativo.");

        return new ImedtoPlano
        {
            Id = Guid.NewGuid(),
            Nome = nome.Trim(),
            DescricaoCurta = descricaoCurta?.Trim(),
            PrecoMensalCentavos = precoMensalCentavos,
            Gratuito = gratuito,
            Ativo = true,
            LimitesJson = string.IsNullOrWhiteSpace(limitesJson) ? "{}" : limitesJson,
            FeaturesJson = string.IsNullOrWhiteSpace(featuresJson) ? "{}" : featuresJson,
            CriadoEm = DateTimeOffset.UtcNow,
            CriadoPorAdminId = criadoPorAdminId
        };
    }

    public virtual void Atualizar(
        string nome,
        string? descricaoCurta,
        int? precoMensalCentavos,
        bool gratuito,
        string limitesJson,
        string featuresJson = "{}")
    {
        ValidarNome(nome);
        if (precoMensalCentavos.HasValue && precoMensalCentavos.Value < 0)
            throw new BusinessException("Preço mensal não pode ser negativo.");

        Nome = nome.Trim();
        DescricaoCurta = descricaoCurta?.Trim();
        PrecoMensalCentavos = precoMensalCentavos;
        Gratuito = gratuito;
        LimitesJson = string.IsNullOrWhiteSpace(limitesJson) ? "{}" : limitesJson;
        FeaturesJson = string.IsNullOrWhiteSpace(featuresJson) ? "{}" : featuresJson;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    public virtual void Inativar()
    {
        if (!Ativo) throw new BusinessException("Plano já está inativo.");
        Ativo = false;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Ativo) throw new BusinessException("Plano já está ativo.");
        Ativo = true;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Verifica se o plano contém a feature informada. FeaturesJson usa formato
    /// {"ia": true, "receitas": false} — dict de bool. Comparação case-insensitive.
    /// Política fail-closed: JSON malformado ou feature ausente = false (não libera).
    /// </summary>
    public bool TemFeature(string feature)
    {
        if (string.IsNullOrWhiteSpace(feature)) return false;
        if (string.IsNullOrWhiteSpace(FeaturesJson) || FeaturesJson == "{}") return false;

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, bool>>(FeaturesJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dict is null) return false;
            // Busca case-insensitive na chave do dicionário.
            foreach (var kv in dict)
                if (string.Equals(kv.Key, feature, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Limite de profissionais do plano. Null = ilimitado.</summary>
    public int? ObterLimiteProfissionais() => ObterLimiteDoJson("profissionais");

    /// <summary>Limite de pacientes do plano. Null = ilimitado.</summary>
    public int? ObterLimitePacientes() => ObterLimiteDoJson("pacientes");

    private int? ObterLimiteDoJson(string chave)
    {
        if (string.IsNullOrWhiteSpace(LimitesJson) || LimitesJson == "{}") return null;

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(LimitesJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dict is null) return null;
            foreach (var kv in dict)
            {
                if (!string.Equals(kv.Key, chave, StringComparison.OrdinalIgnoreCase)) continue;
                if (kv.Value.ValueKind == JsonValueKind.Null) return null;
                if (kv.Value.TryGetInt32(out var val)) return val;
                return null;
            }
            return null;
        }
        catch (JsonException)
        {
            return null; // fail-open em leitura de limite (ilimitado em dúvida)
        }
    }

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do plano é obrigatório.");
        if (nome.Length > 100)
            throw new BusinessException("Nome do plano não pode exceder 100 caracteres.");
    }
}
