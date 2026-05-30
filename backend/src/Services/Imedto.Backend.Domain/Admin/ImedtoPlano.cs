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

    /// <summary>Limites e configurações em JSON (ex: {"profissionais":5,"pacientes":100}).</summary>
    public virtual string LimitesJson { get; protected set; } = "{}";

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
        Guid? criadoPorAdminId)
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
            CriadoEm = DateTimeOffset.UtcNow,
            CriadoPorAdminId = criadoPorAdminId
        };
    }

    public virtual void Atualizar(
        string nome,
        string? descricaoCurta,
        int? precoMensalCentavos,
        bool gratuito,
        string limitesJson)
    {
        ValidarNome(nome);
        if (precoMensalCentavos.HasValue && precoMensalCentavos.Value < 0)
            throw new BusinessException("Preço mensal não pode ser negativo.");

        Nome = nome.Trim();
        DescricaoCurta = descricaoCurta?.Trim();
        PrecoMensalCentavos = precoMensalCentavos;
        Gratuito = gratuito;
        LimitesJson = string.IsNullOrWhiteSpace(limitesJson) ? "{}" : limitesJson;
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

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do plano é obrigatório.");
        if (nome.Length > 100)
            throw new BusinessException("Nome do plano não pode exceder 100 caracteres.");
    }
}
