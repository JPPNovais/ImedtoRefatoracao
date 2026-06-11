using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Inventario;

/// <summary>
/// Registro imutável de movimentação de estoque.
/// Criado sempre pelo aggregate ItemInventario.
/// </summary>
public class MovimentacaoEstoque : Entity, ISoftDeletable
{
    public virtual long ItemInventarioId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual TipoMovimentacaoEstoque Tipo { get; protected set; }
    public virtual decimal Quantidade { get; protected set; }
    public virtual decimal QuantidadeAnterior { get; protected set; }
    public virtual decimal QuantidadeApos { get; protected set; }
    /// <summary>Custo unitário no momento da movimentação (snapshot — em saída, é o CustoMedio do item).</summary>
    public virtual decimal CustoUnitario { get; protected set; }
    /// <summary>Total monetário da movimentação = <see cref="Quantidade"/> × <see cref="CustoUnitario"/>.</summary>
    public virtual decimal CustoTotal { get; protected set; }
    public virtual string? Observacao { get; protected set; }
    /// <summary>
    /// F7/R21 — vínculo da baixa automática à cobrança que a originou.
    /// Nulo para movimentações manuais. Gravado pelo handler de F4/F5.
    /// </summary>
    public virtual long? CobrancaId { get; protected set; }
    public virtual Guid CriadoPorUsuarioId { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }

    // Soft delete — protege histórico contábil (LGPD + integridade financeira).
    public virtual DateTime? DeletadoEm { get; protected set; }
    public virtual Guid? DeletadoPorUsuarioId { get; protected set; }

    protected MovimentacaoEstoque() { }

    internal static MovimentacaoEstoque Criar(
        long itemInventarioId,
        long estabelecimentoId,
        TipoMovimentacaoEstoque tipo,
        decimal quantidade,
        decimal quantidadeAnterior,
        decimal quantidadeApos,
        Guid criadoPorUsuarioId,
        decimal custoUnitario,
        string? observacao,
        long? cobrancaId = null)
    {
        return new MovimentacaoEstoque
        {
            ItemInventarioId = itemInventarioId,
            EstabelecimentoId = estabelecimentoId,
            Tipo = tipo,
            Quantidade = quantidade,
            QuantidadeAnterior = quantidadeAnterior,
            QuantidadeApos = quantidadeApos,
            CustoUnitario = custoUnitario,
            CustoTotal = quantidade * custoUnitario,
            CriadoPorUsuarioId = criadoPorUsuarioId,
            Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
            CobrancaId = cobrancaId,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void MarcarComoDeletado(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário responsável pela exclusão é obrigatório.");
        if (DeletadoEm is not null)
            throw new BusinessException("Movimentação já está deletada.");
        DeletadoEm = DateTime.UtcNow;
        DeletadoPorUsuarioId = usuarioId;
    }
}
