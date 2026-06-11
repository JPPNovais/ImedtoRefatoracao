using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

/// <summary>
/// Preview (GET) para o modal de confirmação da ação MarcarProcedimentoRealizado.
/// Retorna procedimentos + valor total + produtos a baixar + sinalização de sem-vínculo.
/// Não persiste nada — leitura pura.
/// </summary>
public class PreviewProcedimentoRealizadoQuery : IQuery<PreviewProcedimentoRealizadoDto>
{
    public long PendenciaId { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class PreviewProcedimentoRealizadoDto
{
    public long PendenciaId { get; set; }
    public long EvolucaoId { get; set; }
    public List<ProcedimentoPreviewItem> Procedimentos { get; set; } = new();
    public decimal ValorTotal { get; set; }
    public List<ProdutoPreviewItem> ProdutosABaixar { get; set; } = new();
    /// <summary>True se ao menos 1 procedimento tem produto sem item de estoque vinculado.</summary>
    public bool TemProdutoSemVinculo { get; set; }
}

public class ProcedimentoPreviewItem
{
    public long CatalogoCirurgiaId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string? Observacao { get; set; }
}

public class ProdutoPreviewItem
{
    public long ProdutoId { get; set; }
    public string ProdutoNome { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    /// <summary>Null quando produto sem ItemInventarioId — sinalizado no preview (CA94).</summary>
    public long? ItemInventarioId { get; set; }
    public string? ItemInventarioNome { get; set; }
    /// <summary>True quando produto não tem item de estoque vinculado — sinalizar no modal.</summary>
    public bool SemVinculo { get; set; }
}
