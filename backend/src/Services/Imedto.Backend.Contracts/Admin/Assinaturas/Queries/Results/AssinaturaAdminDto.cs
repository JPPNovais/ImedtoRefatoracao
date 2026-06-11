namespace Imedto.Backend.Contracts.Admin.Assinaturas.Queries.Results;

public class AssinaturaAdminDto
{
    public Guid Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid PlanoId { get; set; }
    public string PlanoNome { get; set; } = string.Empty;
    public bool PlanoGratuito { get; set; }
    public DateTimeOffset IniciadaEm { get; set; }
    public DateTimeOffset? FimEm { get; set; }
    public DateTimeOffset? ExpiraEm { get; set; }
    public DateTimeOffset? SuspensaEm { get; set; }
    public bool Gratuita { get; set; }
    public string? Motivo { get; set; }
    public DateTimeOffset CriadaEm { get; set; }
    public bool Vigente { get; set; }
    /// <summary>Estado derivado legível: Vitalicia | Temporaria | Suspensa | Expirada | Encerrada</summary>
    public string Estado { get; set; } = string.Empty;
}
