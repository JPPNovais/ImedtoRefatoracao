using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos;

/// <summary>
/// Implante orçado. Pode estar vinculado ao catálogo de inventário
/// (<see cref="ItemInventarioId"/>) ou ser texto livre — orçamento permite implantes
/// específicos não-catalogados (ex.: implante customizado).
///
/// O custo é snapshot — se o item de inventário mudar de preço depois, o orçamento mantém
/// o valor da época. Não há reconciliação automática.
/// </summary>
public class OrcamentoImplante : Entity
{
    public virtual long OrcamentoId { get; protected set; }
    public virtual long? ItemInventarioId { get; protected set; }
    public virtual string Descricao { get; protected set; } = string.Empty;
    public virtual decimal Quantidade { get; protected set; }
    public virtual decimal CustoUnitario { get; protected set; }
    public virtual decimal CustoTotal { get; protected set; }

    protected OrcamentoImplante() { }

    internal static OrcamentoImplante Criar(
        long orcamentoId,
        long? itemInventarioId,
        string descricao,
        decimal quantidade,
        decimal custoUnitario)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new BusinessException("Descrição do implante é obrigatória.");
        if (descricao.Length > 200)
            throw new BusinessException("Descrição não pode ter mais de 200 caracteres.");
        if (quantidade <= 0)
            throw new BusinessException("Quantidade do implante deve ser maior que zero.");
        if (custoUnitario < 0)
            throw new BusinessException("Custo unitário não pode ser negativo.");

        return new OrcamentoImplante
        {
            OrcamentoId = orcamentoId,
            ItemInventarioId = itemInventarioId,
            Descricao = descricao.Trim(),
            Quantidade = quantidade,
            CustoUnitario = Math.Round(custoUnitario, 4),
            CustoTotal = Math.Round(quantidade * custoUnitario, 4)
        };
    }
}
