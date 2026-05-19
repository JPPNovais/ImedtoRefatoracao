using Imedto.Backend.Domain.Orcamentos.Catalogos;

namespace Imedto.Backend.Domain.Orcamentos.Calculos;

/// <summary>
/// Calculadora pura do orçamento — fonte da verdade dos cálculos que antes viviam
/// no <c>useOrcamentoCalculos.ts</c> do legado. Métodos estáticos sem efeito
/// colateral, todos em <see cref="decimal"/> para evitar erro de aritmética
/// financeira. Frontend chama via endpoint <c>/orcamentos/preview</c>.
/// </summary>
public static class OrcamentoCalculadora
{
    /// <summary>
    /// Calcula honorário de um profissional dado o tempo total da cirurgia.
    /// Regra (paridade legado):
    ///   - Se <paramref name="tempoCirurgiaMinutos"/> &lt;= <paramref name="tempoBaseMinutos"/>:
    ///     honorário = <paramref name="valorTempoBase"/>.
    ///   - Senão: honorário = valorTempoBase
    ///                       + ceil((tempo - tempoBase) / tempoAdicional) * valorAdicional
    ///                       + valorPlus.
    /// </summary>
    public static decimal CalcularValorProfissional(
        int tempoCirurgiaMinutos,
        int tempoBaseMinutos,
        decimal valorTempoBase,
        int tempoAdicionalMinutos,
        decimal valorAdicional,
        decimal valorPlus)
    {
        if (tempoCirurgiaMinutos <= 0) return 0m;
        if (tempoBaseMinutos <= 0) return valorTempoBase + valorPlus;
        if (tempoCirurgiaMinutos <= tempoBaseMinutos)
            return Math.Round(valorTempoBase + valorPlus, 2);

        var excedente = tempoCirurgiaMinutos - tempoBaseMinutos;
        var divisor = tempoAdicionalMinutos > 0 ? tempoAdicionalMinutos : 1;
        // Ceil para que cada minuto excedente que cruze o bloco já cobre o adicional.
        var blocos = (int)Math.Ceiling((double)excedente / divisor);
        var total = valorTempoBase + (blocos * valorAdicional) + valorPlus;
        return Math.Round(total, 2);
    }

    /// <summary>
    /// Calcula valor de local cirúrgico (sala) por tempo. Mesma estrutura de
    /// "tempo base + blocos adicionais" do honorário profissional.
    /// Sobrecarga primitiva — mantida para uso direto e nos testes unitários.
    /// </summary>
    public static decimal CalcularValorLocal(
        int tempoCirurgiaMinutos,
        int tempoBaseMinutos,
        decimal valorBase,
        int tempoAdicionalMinutos,
        decimal valorAdicional)
    {
        if (tempoCirurgiaMinutos <= 0) return 0m;
        if (tempoBaseMinutos <= 0) return valorBase;
        if (tempoCirurgiaMinutos <= tempoBaseMinutos)
            return Math.Round(valorBase, 2);

        var excedente = tempoCirurgiaMinutos - tempoBaseMinutos;
        var divisor = tempoAdicionalMinutos > 0 ? tempoAdicionalMinutos : 1;
        var blocos = (int)Math.Ceiling((double)excedente / divisor);
        return Math.Round(valorBase + (blocos * valorAdicional), 2);
    }

    /// <summary>
    /// Calcula valor de local cirúrgico dado um <see cref="TipoLocalCirurgia"/>, o
    /// tempo total da cirurgia em minutos e a <see cref="ConfiguracaoLocalCirurgia"/>
    /// daquele tipo no estabelecimento. Para <c>SemInternacao</c> e <c>Ambulatorio</c>
    /// devolve o valor fixo da config (independe do tempo). Quando a config é null,
    /// retorna 0 (estabelecimento ainda não configurou).
    /// </summary>
    public static decimal CalcularValorLocal(
        TipoLocalCirurgia tipo,
        int tempoCirurgiaMinutos,
        ConfiguracaoLocalCirurgia? config)
    {
        if (config is null) return 0m;

        // Tipos fixos — valor independe do tempo.
        if (tipo is TipoLocalCirurgia.SemInternacao or TipoLocalCirurgia.Ambulatorio)
            return Math.Round(config.ValorBase, 2);

        return CalcularValorLocal(
            tempoCirurgiaMinutos,
            config.TempoBaseMinutos,
            config.ValorBase,
            config.TempoAdicionalMinutos,
            config.ValorAdicional);
    }

    /// <summary>
    /// Calcula totais de uma forma de pagamento sobre o subtotal do orçamento.
    /// <list type="bullet">
    ///   <item><c>TotalBruto</c> = subtotal × (1 + acréscimo/100).</item>
    ///   <item><c>Entrada</c> = totalBruto × (entrada/100).</item>
    ///   <item><c>ValorParcela</c> = (totalBruto − entrada) / parcelas, ou 0 se parcelas ≤ 0.</item>
    /// </list>
    /// </summary>
    public static FormaPagamentoCalculada CalcularFormaPagamento(
        decimal subtotal,
        decimal acrescimoPercentual,
        decimal entradaPercentual,
        int parcelas)
    {
        var totalBruto = Math.Round(subtotal * (1 + acrescimoPercentual / 100m), 2);
        var entrada = Math.Round(totalBruto * (entradaPercentual / 100m), 2);
        var restante = totalBruto - entrada;
        var valorParcela = parcelas > 0 ? Math.Round(restante / parcelas, 2) : 0m;
        return new FormaPagamentoCalculada(totalBruto, entrada, valorParcela);
    }
}

/// <summary>Resultado do cálculo de uma forma de pagamento.</summary>
public record FormaPagamentoCalculada(decimal TotalBruto, decimal Entrada, decimal ValorParcela);
