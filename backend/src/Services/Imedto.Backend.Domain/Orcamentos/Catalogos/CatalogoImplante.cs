using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

/// <summary>
/// Catálogo de implantes para uso em orçamentos. Pode opcionalmente referenciar um
/// item de inventário via <see cref="ItemInventarioId"/> — quando vinculado, o preço
/// do orçamento pode partir do custo unitário do inventário (mas o catálogo mantém
/// o valor "comercial" próprio).
/// </summary>
public class CatalogoImplante : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual long? ItemInventarioId { get; protected set; }
    public virtual string Descricao { get; protected set; } = string.Empty;
    public virtual decimal CustoUnitario { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected CatalogoImplante() { }

    public static CatalogoImplante Criar(
        long estabelecimentoId,
        long? itemInventarioId,
        string descricao,
        decimal custoUnitario)
    {
        Validar(estabelecimentoId, descricao, custoUnitario);
        return new CatalogoImplante
        {
            EstabelecimentoId = estabelecimentoId,
            ItemInventarioId = itemInventarioId,
            Descricao = descricao.Trim(),
            CustoUnitario = custoUnitario,
            Ativo = true,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(long? itemInventarioId, string descricao, decimal custoUnitario)
    {
        Validar(EstabelecimentoId, descricao, custoUnitario);
        ItemInventarioId = itemInventarioId;
        Descricao = descricao.Trim();
        CustoUnitario = custoUnitario;
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Inativar() { if (Ativo) { Ativo = false; AtualizadaEm = DateTime.UtcNow; } }
    public virtual void Reativar() { if (!Ativo) { Ativo = true; AtualizadaEm = DateTime.UtcNow; } }

    private static void Validar(long estab, string descricao, decimal custo)
    {
        if (estab <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(descricao)) throw new BusinessException("Descrição é obrigatória.");
        if (custo < 0) throw new BusinessException("Custo unitário não pode ser negativo.");
    }
}
