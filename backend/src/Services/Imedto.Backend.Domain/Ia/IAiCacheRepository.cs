namespace Imedto.Backend.Domain.Ia;

public interface IAiCacheRepository
{
    /// <summary>Retorna o output cacheado se existir e não tiver expirado; senão, null.</summary>
    Task<string?> ObterAsync(string promptHash, CancellationToken ct = default);

    /// <summary>Upsert do cache para o hash. <paramref name="expiraEm"/> é absoluto em UTC.</summary>
    Task SalvarAsync(
        string promptHash,
        long estabelecimentoId,
        string endpoint,
        string output,
        DateTime expiraEm,
        int? tokensIn = null,
        int? tokensOut = null,
        CancellationToken ct = default);

    /// <summary>Remove entradas expiradas — placeholder para job de limpeza (Fase 2).</summary>
    Task<int> RemoverExpiradosAsync(CancellationToken ct = default);
}
