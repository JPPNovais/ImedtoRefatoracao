using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos;

/// <summary>
/// Distribuição do valor total do orçamento por forma de pagamento (PIX, cartão, etc.).
/// A soma de <see cref="Valor"/> deve bater exatamente com o total do orçamento (ver
/// <c>Orcamento.ValidarIntegridade</c>) — divergências não são silenciosamente toleradas.
///
/// Item 7 — paridade com legado: além de valor/parcelas/observação, mantemos
/// <see cref="AcrescimoPercentual"/> (juros aplicados na forma) e
/// <see cref="EntradaPercentual"/> (% da forma que é entrada). Esses campos eram
/// perdidos quando a configuração ficava só no JSON opaco.
/// </summary>
public class OrcamentoFormaPagamento : Entity
{
    public virtual long OrcamentoId { get; protected set; }
    public virtual long FormaPagamentoId { get; protected set; }
    public virtual decimal Valor { get; protected set; }
    public virtual int Parcelas { get; protected set; }
    public virtual decimal AcrescimoPercentual { get; protected set; }
    public virtual decimal EntradaPercentual { get; protected set; }
    public virtual string? Observacao { get; protected set; }
    public virtual int Ordem { get; protected set; }

    protected OrcamentoFormaPagamento() { }

    internal static OrcamentoFormaPagamento Criar(
        long orcamentoId,
        long formaPagamentoId,
        decimal valor,
        int parcelas,
        decimal acrescimoPercentual,
        decimal entradaPercentual,
        string? observacao,
        int ordem)
    {
        if (formaPagamentoId <= 0)
            throw new BusinessException("Forma de pagamento é obrigatória.");
        if (valor <= 0)
            throw new BusinessException("Valor da forma de pagamento deve ser maior que zero.");
        if (parcelas <= 0)
            throw new BusinessException("Quantidade de parcelas deve ser maior que zero.");
        if (parcelas > 60)
            throw new BusinessException("Quantidade de parcelas não pode exceder 60.");
        if (acrescimoPercentual < 0)
            throw new BusinessException("Acréscimo percentual não pode ser negativo.");
        if (acrescimoPercentual > 100)
            throw new BusinessException("Acréscimo percentual não pode exceder 100%.");
        if (entradaPercentual < 0)
            throw new BusinessException("Entrada percentual não pode ser negativa.");
        if (entradaPercentual > 100)
            throw new BusinessException("Entrada percentual não pode exceder 100%.");
        if (observacao is { Length: > 200 })
            throw new BusinessException("Observação não pode ter mais de 200 caracteres.");
        if (ordem < 0)
            throw new BusinessException("Ordem não pode ser negativa.");

        return new OrcamentoFormaPagamento
        {
            OrcamentoId = orcamentoId,
            FormaPagamentoId = formaPagamentoId,
            Valor = Math.Round(valor, 2),
            Parcelas = parcelas,
            AcrescimoPercentual = Math.Round(acrescimoPercentual, 2),
            EntradaPercentual = Math.Round(entradaPercentual, 2),
            Observacao = observacao?.Trim(),
            Ordem = ordem
        };
    }
}
