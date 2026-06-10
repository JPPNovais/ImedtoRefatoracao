namespace Imedto.Backend.Contracts.Cobrancas.Queries.Results;

public class ValorSugeridoCheckInDto
{
    /// <summary>null = nenhum preço configurado (CA16 — campo vazio com hint).</summary>
    public decimal? ValorSugerido { get; set; }
}
