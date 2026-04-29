namespace Imedto.Backend.Infrastructure.Ia;

/// <summary>
/// Configurações da IA. Bind da seção <c>Ia</c> de <c>appsettings.json</c>.
/// </summary>
public class IaOptions
{
    public const string Section = "Ia";

    /// <summary>Chamadas permitidas por usuário em uma janela de 60 segundos.</summary>
    public int LimitePorMinuto { get; set; } = 10;

    /// <summary>Tempo de vida do cache de outputs antes de expirar.</summary>
    public int CacheTtlHoras { get; set; } = 24;
}
