namespace Imedto.Backend.Domain.Ia;

public interface IAiRateLimitRepository
{
    /// <summary>
    /// Registra uma tentativa de uso da IA pelo usuário na janela de 1 minuto atual.
    /// Retorna <c>true</c> se a chamada está dentro do limite; <c>false</c> se excedeu.
    /// Implementação faz upsert atômico para evitar corrida.
    /// </summary>
    Task<bool> RegistrarTentativaAsync(
        Guid usuarioId,
        int limitePorMinuto,
        CancellationToken ct = default);
}
