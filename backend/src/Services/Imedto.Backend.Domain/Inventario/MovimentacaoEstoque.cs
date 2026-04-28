using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Inventario;

/// <summary>
/// Registro imutável de movimentação de estoque.
/// Criado sempre pelo aggregate ItemInventario.
/// </summary>
public class MovimentacaoEstoque : Entity
{
    public virtual long ItemInventarioId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual TipoMovimentacaoEstoque Tipo { get; protected set; }
    public virtual decimal Quantidade { get; protected set; }
    public virtual decimal QuantidadeAnterior { get; protected set; }
    public virtual decimal QuantidadeApos { get; protected set; }
    public virtual string? Observacao { get; protected set; }
    public virtual Guid CriadoPorUsuarioId { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }

    protected MovimentacaoEstoque() { }

    internal static MovimentacaoEstoque Criar(
        long itemInventarioId,
        long estabelecimentoId,
        TipoMovimentacaoEstoque tipo,
        decimal quantidade,
        decimal quantidadeAnterior,
        decimal quantidadeApos,
        Guid criadoPorUsuarioId,
        string? observacao)
    {
        return new MovimentacaoEstoque
        {
            ItemInventarioId = itemInventarioId,
            EstabelecimentoId = estabelecimentoId,
            Tipo = tipo,
            Quantidade = quantidade,
            QuantidadeAnterior = quantidadeAnterior,
            QuantidadeApos = quantidadeApos,
            CriadoPorUsuarioId = criadoPorUsuarioId,
            Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
            CriadoEm = DateTime.UtcNow
        };
    }
}
