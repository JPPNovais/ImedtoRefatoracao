namespace Imedto.Backend.Contracts.Admin.Catalogos.Variaveis.Queries.Results;

public class VariavelGlobalListaItemDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? AtualizadoEm { get; set; }
}

public class VariavelGlobalDetalheDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? ValoresJson { get; set; }
    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? AtualizadoEm { get; set; }
    public Guid? CriadoPorAdminId { get; set; }
    public Guid? AtualizadoPorAdminId { get; set; }
}
