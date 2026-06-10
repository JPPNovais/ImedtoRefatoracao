namespace Imedto.Backend.Domain.Cobrancas;

/// <summary>
/// Helper único de arredondamento monetário do domínio (R13).
/// Todo cálculo de taxa/desconto/saldo passa por aqui.
/// 2 casas decimais, MidpointRounding.AwayFromZero — nunca float/double.
/// </summary>
public static class ArredondamentoMonetario
{
    public static decimal Arredondar(decimal valor)
        => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
}
