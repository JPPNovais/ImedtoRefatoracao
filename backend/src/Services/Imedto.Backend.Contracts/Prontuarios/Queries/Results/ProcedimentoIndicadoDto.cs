namespace Imedto.Backend.Contracts.Prontuarios.Queries.Results;

/// <summary>
/// Snapshot de um procedimento indicado de uma evolução para pré-preenchimento do orçamento (F5/R2).
/// Apenas itens com catalogoCirurgiaId presente (itens legado texto-livre são excluídos).
/// LGPD: sem dado clínico; apenas id de catálogo, descrição de catálogo e valor.
/// </summary>
public class ProcedimentoIndicadoDto
{
    public long CatalogoCirurgiaId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}
