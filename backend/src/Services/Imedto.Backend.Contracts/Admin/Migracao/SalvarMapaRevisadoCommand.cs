namespace Imedto.Backend.Contracts.Admin.Migracao;

public sealed class SalvarMapaRevisadoCommand
{
    public long JobId { get; init; }
    public string Entidade { get; init; } = string.Empty;
    /// <summary>De-para revisado: coluna_origem → campo_canonico.</summary>
    public Dictionary<string, string> DePara { get; init; } = [];
    public Guid RevisadoPorUsuarioId { get; init; }
}
