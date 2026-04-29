namespace Imedto.Backend.Contracts.Catalogo.Queries.Results;

public class RegiaoCatalogoDto
{
    public long Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nome { get; init; } = string.Empty;
    public string? PaiCodigo { get; init; }
    public short Nivel { get; init; }
    public string? Vista { get; init; }
    public string? TemplateTexto { get; init; }
    public string? SvgCoordsJson { get; init; }
    public short Ordem { get; init; }
    public bool Lateralidade { get; init; }
    public bool Ativo { get; init; }
}
