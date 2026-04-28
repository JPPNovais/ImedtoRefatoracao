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
}
