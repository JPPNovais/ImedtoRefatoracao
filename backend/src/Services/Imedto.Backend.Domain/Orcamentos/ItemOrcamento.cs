using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos;

public class ItemOrcamento : Entity
{
    public virtual long OrcamentoId { get; protected set; }
    public virtual string Descricao { get; protected set; } = string.Empty;
    public virtual decimal Quantidade { get; protected set; }
    public virtual decimal ValorUnitario { get; protected set; }
    public virtual decimal DescontoPercent { get; protected set; }
    public virtual decimal Subtotal { get; protected set; }

    protected ItemOrcamento() { }

    internal static ItemOrcamento Criar(
        long orcamentoId,
        string descricao,
        decimal quantidade,
        decimal valorUnitario,
        decimal descontoPercent)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new BusinessException("Descrição do item é obrigatória.");
        if (quantidade <= 0)
            throw new BusinessException("Quantidade do item deve ser maior que zero.");
        if (valorUnitario < 0)
            throw new BusinessException("Valor unitário não pode ser negativo.");
        if (descontoPercent < 0 || descontoPercent > 100)
            throw new BusinessException("Desconto deve estar entre 0 e 100%.");

        var subtotal = quantidade * valorUnitario * (1 - descontoPercent / 100m);
        return new ItemOrcamento
        {
            OrcamentoId = orcamentoId,
            Descricao = descricao.Trim(),
            Quantidade = quantidade,
            ValorUnitario = valorUnitario,
            DescontoPercent = descontoPercent,
            Subtotal = Math.Round(subtotal, 2)
        };
    }
}
