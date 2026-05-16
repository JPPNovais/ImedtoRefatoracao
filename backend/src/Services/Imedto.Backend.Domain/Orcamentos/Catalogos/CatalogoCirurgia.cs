using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

/// <summary>
/// Catálogo de cirurgias (UI nova: "Procedimento") para uso em orçamentos.
/// </summary>
public class CatalogoCirurgia : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Descricao { get; protected set; } = string.Empty;
    public virtual decimal ValorBase { get; protected set; }
    public virtual int? DuracaoPadraoMinutos { get; protected set; }
    public virtual string? CodigoInterno { get; protected set; }
    public virtual string? CodigoTuss { get; protected set; }
    public virtual string? Categoria { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected CatalogoCirurgia() { }

    public static CatalogoCirurgia Criar(
        long estabelecimentoId, string descricao, decimal valorBase,
        int? duracaoPadraoMinutos, string? codigoInterno = null,
        string? codigoTuss = null, string? categoria = null)
    {
        Validar(estabelecimentoId, descricao, valorBase, duracaoPadraoMinutos, codigoInterno, codigoTuss, categoria);
        return new CatalogoCirurgia
        {
            EstabelecimentoId = estabelecimentoId,
            Descricao = descricao.Trim(),
            ValorBase = valorBase,
            DuracaoPadraoMinutos = duracaoPadraoMinutos,
            CodigoInterno = N(codigoInterno),
            CodigoTuss = N(codigoTuss),
            Categoria = N(categoria),
            Ativo = true,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(string descricao, decimal valorBase, int? duracaoPadraoMinutos,
        string? codigoInterno = null, string? codigoTuss = null, string? categoria = null)
    {
        Validar(EstabelecimentoId, descricao, valorBase, duracaoPadraoMinutos, codigoInterno, codigoTuss, categoria);
        Descricao = descricao.Trim();
        ValorBase = valorBase;
        DuracaoPadraoMinutos = duracaoPadraoMinutos;
        CodigoInterno = N(codigoInterno);
        CodigoTuss = N(codigoTuss);
        Categoria = N(categoria);
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Inativar() { if (Ativo) { Ativo = false; AtualizadaEm = DateTime.UtcNow; } }
    public virtual void Reativar() { if (!Ativo) { Ativo = true; AtualizadaEm = DateTime.UtcNow; } }

    private static string? N(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private static void Validar(long e, string descricao, decimal valorBase, int? duracao,
        string? codigoInterno, string? codigoTuss, string? categoria)
    {
        if (e <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(descricao)) throw new BusinessException("Descrição da cirurgia é obrigatória.");
        if (valorBase < 0) throw new BusinessException("Valor base não pode ser negativo.");
        if (duracao is { } d && d <= 0) throw new BusinessException("Duração padrão deve ser positiva.");
        if (codigoInterno is not null && codigoInterno.Trim().Length > 40)
            throw new BusinessException("Código interno não pode ter mais de 40 caracteres.");
        if (codigoTuss is not null && codigoTuss.Trim().Length > 20)
            throw new BusinessException("Código TUSS não pode ter mais de 20 caracteres.");
        if (categoria is not null && categoria.Trim().Length > 80)
            throw new BusinessException("Categoria não pode ter mais de 80 caracteres.");
    }
}
