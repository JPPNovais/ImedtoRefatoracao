namespace Imedto.Backend.Contracts.Admin.Catalogos.Regioes.Queries.Results;

public class RegiaoGlobalListaItemDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string[]? Sinonimos { get; set; }
    public string? SistemaCorporal { get; set; }
    public bool Ativo { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? AtualizadoEm { get; set; }
}

public class RegiaoGlobalDetalheDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string[]? Sinonimos { get; set; }
    public string? SistemaCorporal { get; set; }
    public bool Ativo { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? AtualizadoEm { get; set; }
}
