namespace Imedto.Backend.Domain.Financeiro;

/// <summary>
/// Constantes de configuração do cálculo de comissão (R15).
/// Centralizado aqui para que Domain, Application e Infrastructure referenciem o mesmo valor.
/// </summary>
public static class ComissaoConfig
{
    /// <summary>
    /// Percentual de comissão padrão do sistema (30%) quando não há
    /// <see cref="ConfigComissaoProfissional"/> explícita para o profissional+tipo (R15).
    /// </summary>
    public const decimal PercentualPadrao = 30m;
}
