namespace Imedto.Backend.Contracts.Admin.Catalogos.Modelos.Queries.Results;

public class ModeloGlobalListaItemDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? AtualizadoEm { get; set; }
}

public class ModeloGlobalDetalheDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string ConteudoJson { get; set; } = "{}";
    public bool Ativo { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? AtualizadoEm { get; set; }
    public Guid? CriadoPorAdminId { get; set; }
    public Guid? AtualizadoPorAdminId { get; set; }
}
