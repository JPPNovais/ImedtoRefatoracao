using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

public class OrcamentoPacoteProduto : Entity
{
    public virtual long PacoteId { get; protected set; }
    public virtual long CatalogoProdutoId { get; protected set; }
    public virtual decimal Quantidade { get; protected set; }

    protected OrcamentoPacoteProduto() { }

    internal static OrcamentoPacoteProduto Criar(long catalogoProdutoId, decimal quantidade)
    {
        if (catalogoProdutoId <= 0) throw new BusinessException("Produto é obrigatório.");
        if (quantidade <= 0) throw new BusinessException("Quantidade do produto no pacote deve ser positiva.");
        return new OrcamentoPacoteProduto { CatalogoProdutoId = catalogoProdutoId, Quantidade = quantidade };
    }
}
