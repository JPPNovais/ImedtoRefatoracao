using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Ia;

/// <summary>
/// Configurações de IA por estabelecimento (item 2.12). Permite que cada tenant
/// desabilite o assistente, ajuste limites de uso e o nível de minimização de dados.
///
/// Tenant-scoped: o <see cref="Id"/> é o próprio <c>estabelecimento_id</c> (PK = FK).
/// Falta de linha = decorator usa defaults globais de <c>IaOptions</c>.
/// </summary>
public class EstabelecimentoIaSettings : Entity
{
    public virtual bool AiEnabled { get; protected set; }
    public virtual string AiProvider { get; protected set; } = "anthropic";
    public virtual string AiModel { get; protected set; } = "claude-sonnet-4-6";
    public virtual int RateLimitPerMinute { get; protected set; }
    public virtual int RateLimitPerDay { get; protected set; }
    public virtual NivelMinimizacaoDados DataMinimizationLevel { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected EstabelecimentoIaSettings() { }

    /// <summary>
    /// Constrói uma linha de settings com os defaults do schema. Útil para o caso
    /// "PUT /ia-settings" quando o tenant ainda não tem registro persistido.
    /// </summary>
    public static EstabelecimentoIaSettings CriarPadrao(long estabelecimentoId)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento inválido.");

        return new EstabelecimentoIaSettings
        {
            Id                    = estabelecimentoId,
            AiEnabled             = true,
            AiProvider            = "anthropic",
            AiModel               = "claude-sonnet-4-6",
            RateLimitPerMinute    = 10,
            RateLimitPerDay       = 200,
            DataMinimizationLevel = NivelMinimizacaoDados.Standard,
            AtualizadaEm          = null
        };
    }

    public void Habilitar()
    {
        AiEnabled    = true;
        AtualizadaEm = DateTime.UtcNow;
    }

    public void Desabilitar()
    {
        AiEnabled    = false;
        AtualizadaEm = DateTime.UtcNow;
    }

    public void AtualizarLimites(int porMinuto, int porDia)
    {
        if (porMinuto <= 0)
            throw new BusinessException("Limite por minuto deve ser maior que zero.");
        if (porDia <= 0)
            throw new BusinessException("Limite por dia deve ser maior que zero.");
        if (porDia < porMinuto)
            throw new BusinessException("Limite diário deve ser maior ou igual ao limite por minuto.");

        RateLimitPerMinute = porMinuto;
        RateLimitPerDay    = porDia;
        AtualizadaEm       = DateTime.UtcNow;
    }

    public void AtualizarModelo(string provider, string model)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new BusinessException("Provedor de IA é obrigatório.");
        if (string.IsNullOrWhiteSpace(model))
            throw new BusinessException("Modelo de IA é obrigatório.");
        if (provider.Length > 40)
            throw new BusinessException("Provedor de IA inválido.");
        if (model.Length > 80)
            throw new BusinessException("Modelo de IA inválido.");

        AiProvider   = provider.Trim();
        AiModel      = model.Trim();
        AtualizadaEm = DateTime.UtcNow;
    }

    public void AtualizarMinimizacao(NivelMinimizacaoDados nivel)
    {
        DataMinimizationLevel = nivel;
        AtualizadaEm          = DateTime.UtcNow;
    }
}
