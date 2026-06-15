namespace Imedto.Backend.Contracts.Admin.Migracao;

/// <summary>
/// Comando de aprovação manual da análise por IA de um job de migração.
/// Addendum 003 — R-A2/CA41. Apenas ImedtoAdmin (R-A4).
/// </summary>
public sealed class AprovarAnaliseCommand
{
    public long JobId { get; init; }

    /// <summary>ID do admin que está aprovando (para audit — R-A6).</summary>
    public Guid AdminId { get; init; }
}
