namespace Imedto.Backend.Contracts.Catalogo.Queries.Results;

public class ProcedimentoCatalogoDto
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public string? Capitulo { get; set; }
}
