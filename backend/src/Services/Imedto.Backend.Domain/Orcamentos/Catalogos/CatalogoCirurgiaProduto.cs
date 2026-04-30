using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

/// <summary>
/// Vínculo entre uma cirurgia do catálogo e um produto do catálogo. Define
/// <see cref="QuantidadePadrao"/> que é sugerida ao adicionar a cirurgia em um
/// orçamento. <see cref="Obrigatorio"/> = true sinaliza que o produto deve estar
/// presente no orçamento (UI pré-marca; backend não bloqueia ainda).
///
/// O comportamento <c>UsoUnico</c> mora no <see cref="CatalogoProduto"/> — não é
/// duplicado aqui para evitar divergência entre vínculos da mesma cirurgia.
/// </summary>
public class CatalogoCirurgiaProduto : Entity
{
    public virtual long CatalogoCirurgiaId { get; protected set; }
    public virtual long CatalogoProdutoId { get; protected set; }
    public virtual decimal QuantidadePadrao { get; protected set; }
    public virtual bool Obrigatorio { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }

    protected CatalogoCirurgiaProduto() { }

    public static CatalogoCirurgiaProduto Criar(
        long catalogoCirurgiaId,
        long catalogoProdutoId,
        decimal quantidadePadrao,
        bool obrigatorio)
    {
        if (catalogoCirurgiaId <= 0)
            throw new BusinessException("Cirurgia é obrigatória.");
        if (catalogoProdutoId <= 0)
            throw new BusinessException("Produto é obrigatório.");
        if (quantidadePadrao <= 0)
            throw new BusinessException("Quantidade padrão deve ser positiva.");

        return new CatalogoCirurgiaProduto
        {
            CatalogoCirurgiaId = catalogoCirurgiaId,
            CatalogoProdutoId = catalogoProdutoId,
            QuantidadePadrao = quantidadePadrao,
            Obrigatorio = obrigatorio,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void AtualizarQuantidade(decimal quantidadePadrao, bool obrigatorio)
    {
        if (quantidadePadrao <= 0)
            throw new BusinessException("Quantidade padrão deve ser positiva.");
        QuantidadePadrao = quantidadePadrao;
        Obrigatorio = obrigatorio;
    }
}
