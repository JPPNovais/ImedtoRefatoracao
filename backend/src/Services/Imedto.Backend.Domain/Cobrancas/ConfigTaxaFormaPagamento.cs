using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Cobrancas;

/// <summary>
/// Taxa percentual por forma de pagamento (R10).
/// A taxa é derivada daqui no ato do pagamento — nunca digitada manualmente.
/// </summary>
public class ConfigTaxaFormaPagamento : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long FormaPagamentoId { get; protected set; }
    public virtual decimal TaxaPercentual { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected ConfigTaxaFormaPagamento() { }

    public static ConfigTaxaFormaPagamento Criar(
        long estabelecimentoId,
        long formaPagamentoId,
        decimal taxaPercentual)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (formaPagamentoId <= 0)
            throw new BusinessException("Forma de pagamento é obrigatória.");
        if (taxaPercentual < 0)
            throw new BusinessException("Taxa percentual não pode ser negativa.");

        return new ConfigTaxaFormaPagamento
        {
            EstabelecimentoId = estabelecimentoId,
            FormaPagamentoId = formaPagamentoId,
            TaxaPercentual = ArredondamentoMonetario.Arredondar(taxaPercentual),
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(decimal taxaPercentual)
    {
        if (taxaPercentual < 0)
            throw new BusinessException("Taxa percentual não pode ser negativa.");
        TaxaPercentual = ArredondamentoMonetario.Arredondar(taxaPercentual);
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Inativar()
    {
        if (!Ativo) return;
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Ativo) return;
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Calcula a taxa sobre o valor informado (R10/CA18).</summary>
    public decimal CalcularTaxa(decimal valor)
        => ArredondamentoMonetario.Arredondar(valor * TaxaPercentual / 100m);
}
