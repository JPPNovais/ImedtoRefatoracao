using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

/// <summary>
/// Catálogo de produtos para uso em orçamentos. Independente do inventário/estoque —
/// pode haver itens "externos" (fornecidos pelo paciente, por exemplo). Quando
/// <see cref="UsoUnico"/> = true, o produto é cobrado uma única vez por orçamento
/// mesmo que apareça em múltiplas cirurgias (ex: anestesia, cobrança fixa de sala).
/// </summary>
public class CatalogoProduto : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual string? Descricao { get; protected set; }
    public virtual decimal? ValorReferencia { get; protected set; }
    public virtual bool UsoUnico { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected CatalogoProduto() { }

    public static CatalogoProduto Criar(
        long estabelecimentoId,
        string nome,
        string? descricao,
        decimal? valorReferencia,
        bool usoUnico)
    {
        Validar(estabelecimentoId, nome, valorReferencia);
        return new CatalogoProduto
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = nome.Trim(),
            Descricao = descricao?.Trim(),
            ValorReferencia = valorReferencia,
            UsoUnico = usoUnico,
            Ativo = true,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(string nome, string? descricao, decimal? valorReferencia, bool usoUnico)
    {
        Validar(EstabelecimentoId, nome, valorReferencia);
        Nome = nome.Trim();
        Descricao = descricao?.Trim();
        ValorReferencia = valorReferencia;
        UsoUnico = usoUnico;
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Inativar() { if (Ativo) { Ativo = false; AtualizadaEm = DateTime.UtcNow; } }
    public virtual void Reativar() { if (!Ativo) { Ativo = true; AtualizadaEm = DateTime.UtcNow; } }

    private static void Validar(long estab, string nome, decimal? valor)
    {
        if (estab <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome)) throw new BusinessException("Nome do produto é obrigatório.");
        if (valor is { } v && v < 0) throw new BusinessException("Valor de referência não pode ser negativo.");
    }
}
