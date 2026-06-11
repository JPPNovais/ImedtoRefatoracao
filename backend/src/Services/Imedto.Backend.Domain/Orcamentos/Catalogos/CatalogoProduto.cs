using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

public class CatalogoProduto : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    /// <summary>
    /// Vínculo opcional com item de inventário para baixa automática (F4/addendum).
    /// Espelha o padrão de <see cref="CatalogoImplante.ItemInventarioId"/>.
    /// </summary>
    public virtual long? ItemInventarioId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual string? Descricao { get; protected set; }
    public virtual decimal? ValorReferencia { get; protected set; }
    public virtual bool UsoUnico { get; protected set; }
    public virtual TipoOrcamentoProduto Tipo { get; protected set; }
    public virtual string? Marca { get; protected set; }
    public virtual string Unidade { get; protected set; } = "un";
    public virtual string? FornecedorNome { get; protected set; }
    public virtual string? CodigoSku { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected CatalogoProduto() { }

    public static CatalogoProduto Criar(long estabelecimentoId, string nome, string? descricao,
        decimal? valorReferencia, bool usoUnico,
        TipoOrcamentoProduto tipo = TipoOrcamentoProduto.Outros,
        string? marca = null, string? unidade = null,
        string? fornecedorNome = null, string? codigoSku = null,
        long? itemInventarioId = null)
    {
        Validar(estabelecimentoId, nome, valorReferencia, marca, unidade, fornecedorNome, codigoSku);
        return new CatalogoProduto
        {
            EstabelecimentoId = estabelecimentoId,
            ItemInventarioId = itemInventarioId,
            Nome = nome.Trim(),
            Descricao = N(descricao),
            ValorReferencia = valorReferencia,
            UsoUnico = usoUnico,
            Tipo = tipo,
            Marca = N(marca),
            Unidade = NU(unidade),
            FornecedorNome = N(fornecedorNome),
            CodigoSku = N(codigoSku),
            Ativo = true,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(string nome, string? descricao, decimal? valorReferencia,
        bool usoUnico, TipoOrcamentoProduto tipo = TipoOrcamentoProduto.Outros,
        string? marca = null, string? unidade = null,
        string? fornecedorNome = null, string? codigoSku = null,
        long? itemInventarioId = null)
    {
        Validar(EstabelecimentoId, nome, valorReferencia, marca, unidade, fornecedorNome, codigoSku);
        ItemInventarioId = itemInventarioId;
        Nome = nome.Trim();
        Descricao = N(descricao);
        ValorReferencia = valorReferencia;
        UsoUnico = usoUnico;
        Tipo = tipo;
        Marca = N(marca);
        Unidade = NU(unidade);
        FornecedorNome = N(fornecedorNome);
        CodigoSku = N(codigoSku);
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Inativar() { if (Ativo) { Ativo = false; AtualizadaEm = DateTime.UtcNow; } }
    public virtual void Reativar() { if (!Ativo) { Ativo = true; AtualizadaEm = DateTime.UtcNow; } }

    private static string? N(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static string NU(string? u) => string.IsNullOrWhiteSpace(u) ? "un" : u!.Trim();

    private static void Validar(long estab, string nome, decimal? valor,
        string? marca, string? unidade, string? fornecedor, string? sku)
    {
        if (estab <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome)) throw new BusinessException("Nome do produto é obrigatório.");
        if (valor is { } v && v < 0) throw new BusinessException("Valor de referência não pode ser negativo.");
        if (marca is not null && marca.Trim().Length > 120) throw new BusinessException("Marca não pode ter mais de 120 caracteres.");
        if (unidade is not null && unidade.Trim().Length > 20) throw new BusinessException("Unidade não pode ter mais de 20 caracteres.");
        if (fornecedor is not null && fornecedor.Trim().Length > 200) throw new BusinessException("Fornecedor não pode ter mais de 200 caracteres.");
        if (sku is not null && sku.Trim().Length > 40) throw new BusinessException("Código SKU não pode ter mais de 40 caracteres.");
    }
}
