using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

public class CatalogoCirurgiaProduto : Entity
{
    public virtual long CatalogoCirurgiaId { get; protected set; }
    public virtual long CatalogoProdutoId { get; protected set; }
    public virtual decimal QuantidadePadrao { get; protected set; }
    public virtual bool Obrigatorio { get; protected set; }
    public virtual bool Incluido { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }

    protected CatalogoCirurgiaProduto() { }

    public static CatalogoCirurgiaProduto Criar(long catalogoCirurgiaId, long catalogoProdutoId,
        decimal quantidadePadrao, bool obrigatorio, bool incluido = true)
    {
        if (catalogoCirurgiaId <= 0) throw new BusinessException("Cirurgia é obrigatória.");
        if (catalogoProdutoId <= 0) throw new BusinessException("Produto é obrigatório.");
        if (quantidadePadrao <= 0) throw new BusinessException("Quantidade padrão deve ser positiva.");

        return new CatalogoCirurgiaProduto
        {
            CatalogoCirurgiaId = catalogoCirurgiaId,
            CatalogoProdutoId = catalogoProdutoId,
            QuantidadePadrao = quantidadePadrao,
            Obrigatorio = obrigatorio,
            Incluido = incluido,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void AtualizarQuantidade(decimal quantidadePadrao, bool obrigatorio, bool incluido = true)
    {
        if (quantidadePadrao <= 0) throw new BusinessException("Quantidade padrão deve ser positiva.");
        QuantidadePadrao = quantidadePadrao;
        Obrigatorio = obrigatorio;
        Incluido = incluido;
    }
}
