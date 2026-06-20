namespace Imedto.Backend.Contracts.Catalogo.Queries.Results;

public class ExameCatalogoDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Tipo { get; set; }
}
