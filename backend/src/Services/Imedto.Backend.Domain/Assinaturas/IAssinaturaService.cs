namespace Imedto.Backend.Domain.Assinaturas;

/// <summary>
/// Resultado da avaliação de feature gating — distingue motivos de bloqueio para que o
/// frontend escolha entre redirecionar para /assinatura-expirada (assinatura inativa)
/// ou modal de upsell (feature não incluída no plano atual).
/// </summary>
public enum ResultadoFeature
{
    Liberada,
    AssinaturaInativa,
    FeatureNaoIncluida
}

/// <summary>
/// Porta única para gating de feature por tenant. Consumido pelo <c>FeatureGateAttribute</c>
/// e por handlers que precisam validar acesso premium antes de executar lógica de domínio.
///
/// Implementação faz cache curto (1 min) para evitar 1 query por request — o cache é melhor
/// que o NotModifiedSince + ETag aqui porque o "shape" da decisão é booleana e a invalidação
/// natural ocorre quando o dono muda de plano (raro).
/// </summary>
public interface IAssinaturaService
{
    /// <summary>
    /// Indica se o estabelecimento tem direito à feature informada. Política fail-closed:
    /// retorna false se não houver assinatura, se ela estiver em status terminal, ou se o
    /// trial já expirou.
    /// </summary>
    Task<bool> TenantTemFeature(long estabelecimentoId, string feature, CancellationToken ct = default);

    /// <summary>
    /// Avalia detalhadamente o resultado do gating, retornando o motivo do bloqueio.
    /// Permite que callers (FeatureGateAttribute, controllers, frontend) tratem
    /// AssinaturaInativa de forma diferente de FeatureNaoIncluida.
    /// </summary>
    Task<ResultadoFeature> AvaliarFeature(long estabelecimentoId, string feature, CancellationToken ct = default);

    /// <summary>
    /// Indica se o estabelecimento está ativo (Trial dentro do prazo OU Ativa). Usado por telas
    /// que devem bloquear o tenant inteiro (ex: pós-trial sem conversão).
    /// </summary>
    Task<bool> TenantEstaAtivo(long estabelecimentoId, CancellationToken ct = default);

    /// <summary>
    /// Indica se o limite do plano para o <paramref name="recurso"/> já foi atingido.
    /// Política fail-closed: sem assinatura ou sem plano = limite atingido.
    /// Retorna <c>false</c> se o plano tiver limite nulo (ilimitado).
    /// </summary>
    /// <param name="recurso"><c>"profissionais"</c> ou <c>"pacientes"</c>.</param>
    Task<bool> LimiteAtingidoAsync(long estabelecimentoId, string recurso, CancellationToken ct = default);

    /// <summary>
    /// Invalida o cache interno (1 min) das decisões deste estabelecimento.
    /// Chamado quando o status da assinatura muda (upgrade, cancelamento, webhook
    /// de billing) — sem isso, a transição leva até 1 min pra refletir em
    /// <see cref="TenantEstaAtivo"/> e <see cref="AvaliarFeature"/>.
    /// </summary>
    void InvalidarCache(long estabelecimentoId);
}
