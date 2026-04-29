namespace Imedto.Backend.Domain.Ia;

public interface IIaService
{
    IAsyncEnumerable<string> SugerirSecaoProntuarioAsync(
        SugestaoSecaoProntuarioRequest request,
        CancellationToken ct = default);
}

public class SugestaoSecaoProntuarioRequest
{
    public string SecaoAlvoTitulo { get; set; } = string.Empty;
    public Dictionary<string, string> SecoesContexto { get; set; } = new();

    // Item 2.13: ids opcionais que correlacionam a chamada IA com o registro clínico
    // que a motivou. Audit log persiste como FK (ON DELETE SET NULL). Não vão para a IA
    // — são dropados antes da serialização do prompt em RateLimitedIaService.
    public long? PacienteId { get; set; }
    public long? ProntuarioId { get; set; }
    public long? EvolucaoId { get; set; }
}
