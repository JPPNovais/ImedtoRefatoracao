namespace Imedto.Backend.Domain.Ia;

public interface IAiRateLimitRepository
{
    /// <summary>
    /// Registra uma tentativa de uso da IA pelo usuário no estabelecimento informado,
    /// dentro da janela de 1 minuto atual. Retorna <c>true</c> se a chamada está dentro
    /// do limite; <c>false</c> se excedeu. Implementação faz upsert atômico para evitar corrida.
    ///
    /// Item 2.14: a contagem é particionada por (usuario_id, estabelecimento_id) — o mesmo
    /// usuário consumindo em dois tenants tem cotas independentes.
    /// </summary>
    Task<bool> RegistrarTentativaAsync(
        Guid usuarioId,
        long estabelecimentoId,
        int limitePorMinuto,
        CancellationToken ct = default);
}
