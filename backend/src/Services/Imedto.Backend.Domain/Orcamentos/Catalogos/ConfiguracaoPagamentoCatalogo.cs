using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

/// <summary>
/// Configuração padrão de uma forma de pagamento no contexto de orçamentos. Define
/// acréscimo padrão (juros embutidos), entrada padrão (% que vira entrada), e
/// taxa por parcela. Esses valores são "sugestões" — cada
/// <c>OrcamentoFormaPagamento</c> pode ajustá-los individualmente.
/// </summary>
public class ConfiguracaoPagamentoCatalogo : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long FormaPagamentoId { get; protected set; }
    public virtual decimal AcrescimoPercentual { get; protected set; }
    public virtual decimal EntradaPercentualPadrao { get; protected set; }
    public virtual decimal TaxaParcela { get; protected set; }
    public virtual int ParcelasMaximas { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected ConfiguracaoPagamentoCatalogo() { }

    public static ConfiguracaoPagamentoCatalogo Criar(
        long estabelecimentoId,
        long formaPagamentoId,
        decimal acrescimoPercentual,
        decimal entradaPercentualPadrao,
        decimal taxaParcela,
        int parcelasMaximas)
    {
        Validar(estabelecimentoId, formaPagamentoId, acrescimoPercentual, entradaPercentualPadrao, taxaParcela, parcelasMaximas);
        return new ConfiguracaoPagamentoCatalogo
        {
            EstabelecimentoId = estabelecimentoId,
            FormaPagamentoId = formaPagamentoId,
            AcrescimoPercentual = acrescimoPercentual,
            EntradaPercentualPadrao = entradaPercentualPadrao,
            TaxaParcela = taxaParcela,
            ParcelasMaximas = parcelasMaximas,
            Ativo = true,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(
        decimal acrescimoPercentual,
        decimal entradaPercentualPadrao,
        decimal taxaParcela,
        int parcelasMaximas)
    {
        Validar(EstabelecimentoId, FormaPagamentoId, acrescimoPercentual, entradaPercentualPadrao, taxaParcela, parcelasMaximas);
        AcrescimoPercentual = acrescimoPercentual;
        EntradaPercentualPadrao = entradaPercentualPadrao;
        TaxaParcela = taxaParcela;
        ParcelasMaximas = parcelasMaximas;
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Inativar() { if (Ativo) { Ativo = false; AtualizadaEm = DateTime.UtcNow; } }
    public virtual void Reativar() { if (!Ativo) { Ativo = true; AtualizadaEm = DateTime.UtcNow; } }

    private static void Validar(long estab, long formaId, decimal acrescimo, decimal entrada, decimal taxa, int parcelas)
    {
        if (estab <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (formaId <= 0) throw new BusinessException("Forma de pagamento é obrigatória.");
        if (acrescimo < 0) throw new BusinessException("Acréscimo % não pode ser negativo.");
        if (entrada < 0 || entrada > 100) throw new BusinessException("Entrada % deve estar entre 0 e 100.");
        if (taxa < 0) throw new BusinessException("Taxa por parcela não pode ser negativa.");
        if (parcelas <= 0 || parcelas > 60) throw new BusinessException("Parcelas máximas deve estar entre 1 e 60.");
    }
}
