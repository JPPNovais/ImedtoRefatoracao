namespace Imedto.Backend.Contracts.Admin.Migracao;

public sealed class MigracaoMapaDto
{
    public long Id { get; init; }
    public string Entidade { get; init; } = string.Empty;
    /// <summary>JSON com de_para, confianca e duvidas.</summary>
    public string MapaJson { get; init; } = "{}";
    public Guid? RevisadoPorUsuarioId { get; init; }
    public DateTime? RevisadoEm { get; init; }
    public DateTime CriadoEm { get; init; }
}
