namespace Imedto.Backend.Contracts.Admin.Dashboard.Queries.Results;

/// <summary>Item do feed de audit log. POCO class para Dapper.</summary>
public class AuditLogItemDto
{
    public Guid Id { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public Guid? AdminId { get; set; }
    public string? AdminNome { get; set; }
    public string? AdminEmail { get; set; }
    public bool AdminAtivo { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string? RecursoTipo { get; set; }
    public string? RecursoId { get; set; }
    public long? TenantAfetadoId { get; set; }
    public string? TenantNomeFantasia { get; set; }
    public string? Motivo { get; set; }
}

/// <summary>Resultado paginado do feed de audit log. POCO class.</summary>
public class AuditLogPaginadoDto
{
    public IReadOnlyList<AuditLogItemDto> Itens { get; set; } = [];
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}
