using System.Text.Json;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Assinaturas;

/// <summary>
/// Aggregate root de Plano. Catálogo gerenciado pelo seed inicial; criação/edição via console
/// administrativo (ainda não exposto). <see cref="LimiteProfissionais"/>/<see cref="LimitePacientes"/>
/// nulos significam "ilimitado" — alinhado com o schema do doc.
/// </summary>
public class Plano : Entity
{
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual decimal PrecoMensal { get; protected set; }
    public virtual int? LimiteProfissionais { get; protected set; }
    public virtual int? LimitePacientes { get; protected set; }

    /// <summary>JSON array de strings (ex: <c>["receitas","ia"]</c>) — chaves de <see cref="Features"/>.</summary>
    public virtual string FeaturesJson { get; protected set; } = "[]";

    public virtual bool Ativo { get; protected set; }
    public virtual int Ordem { get; protected set; }

    protected Plano() { }

    public static Plano Criar(
        string nome,
        decimal precoMensal,
        int? limiteProfissionais,
        int? limitePacientes,
        IEnumerable<string>? features,
        int ordem = 0)
    {
        ValidarNome(nome);
        ValidarPreco(precoMensal);
        ValidarLimite(limiteProfissionais, "Limite de profissionais");
        ValidarLimite(limitePacientes, "Limite de pacientes");

        return new Plano
        {
            Nome = nome.Trim(),
            PrecoMensal = precoMensal,
            LimiteProfissionais = limiteProfissionais,
            LimitePacientes = limitePacientes,
            FeaturesJson = SerializarFeatures(features),
            Ativo = true,
            Ordem = ordem
        };
    }

    public virtual void Atualizar(
        string nome,
        decimal precoMensal,
        int? limiteProfissionais,
        int? limitePacientes,
        IEnumerable<string>? features,
        int ordem)
    {
        ValidarNome(nome);
        ValidarPreco(precoMensal);
        ValidarLimite(limiteProfissionais, "Limite de profissionais");
        ValidarLimite(limitePacientes, "Limite de pacientes");

        Nome = nome.Trim();
        PrecoMensal = precoMensal;
        LimiteProfissionais = limiteProfissionais;
        LimitePacientes = limitePacientes;
        FeaturesJson = SerializarFeatures(features);
        Ordem = ordem;
    }

    public virtual void Inativar()
    {
        if (!Ativo)
            throw new BusinessException("Plano já está inativo.");
        Ativo = false;
    }

    public virtual void Reativar()
    {
        if (Ativo)
            throw new BusinessException("Plano já está ativo.");
        Ativo = true;
    }

    /// <summary>
    /// Verifica se o plano contém a <paramref name="feature"/> informada. Comparação case-insensitive
    /// para tolerar maiúscula/minúscula nas chaves serializadas. Retorna false se o JSON estiver
    /// malformado — política de "fail-closed" (não libera feature em caso de dado inválido).
    /// </summary>
    public bool TemFeature(string feature)
    {
        if (string.IsNullOrWhiteSpace(feature)) return false;
        if (string.IsNullOrWhiteSpace(FeaturesJson)) return false;

        try
        {
            var lista = JsonSerializer.Deserialize<List<string>>(FeaturesJson) ?? new();
            foreach (var f in lista)
                if (string.Equals(f, feature, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do plano é obrigatório.");
        if (nome.Length > 80)
            throw new BusinessException("Nome do plano não pode exceder 80 caracteres.");
    }

    private static void ValidarPreco(decimal precoMensal)
    {
        if (precoMensal < 0)
            throw new BusinessException("Preço mensal não pode ser negativo.");
    }

    private static void ValidarLimite(int? limite, string nome)
    {
        if (limite.HasValue && limite.Value < 0)
            throw new BusinessException($"{nome} não pode ser negativo.");
    }

    private static string SerializarFeatures(IEnumerable<string>? features)
    {
        if (features is null) return "[]";
        var normalizadas = features
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return JsonSerializer.Serialize(normalizadas);
    }
}
