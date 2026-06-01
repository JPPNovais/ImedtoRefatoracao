using Imedto.Backend.Domain.Receitas;

namespace Imedto.Backend.Domain.AssinaturaDigital;

/// <summary>
/// Resultado do disparo de assinatura no provedor ICP-Brasil.
/// </summary>
public record DisparoAssinaturaResult(
    bool Sucesso,
    string? MensagemErro = null,
    bool ModoHomologacao = false);

/// <summary>
/// Resultado da validação do callback do provedor (webhook).
/// </summary>
public record ValidacaoCallbackResult(
    bool AssinaturaValida,
    bool Sucesso,
    string? PdfBase64 = null,
    string? MensagemErro = null);

/// <summary>
/// Abstração do provedor de assinatura digital ICP-Brasil em nuvem.
/// Implementações concretas: BirdIdAssinaturaProvider (MVP), VIDaaSAssinaturaProvider (futuro).
/// Registrado via config <c>AssinaturaDigital:Provedor</c> — nenhum código fora da infra
/// conhece o tipo concreto.
/// </summary>
public interface IAssinaturaDigitalProvider
{
    /// <summary>
    /// Dispara a assinatura no provedor: autentica com o refresh token do médico,
    /// obtém access token, envia o PDF para ser assinado (PAdES AD_RB).
    /// Retorna resultado indicando sucesso ou falha — não lança exceção em falha
    /// de negócio (ex.: refresh token expirado). Lança apenas em erro de infra.
    /// </summary>
    Task<DisparoAssinaturaResult> DispararAssinaturaAsync(
        Receita receita,
        Guid medicoId,
        string refreshTokenDecifrado,
        CancellationToken ct = default);

    /// <summary>
    /// Valida a autenticidade do callback do provedor via HMAC do header.
    /// Retorna <see cref="ValidacaoCallbackResult.AssinaturaValida"/> false se o
    /// HMAC não bater — caller deve retornar 401 sem mutação.
    /// </summary>
    Task<ValidacaoCallbackResult> ValidarCallbackAsync(
        string payload,
        string headerAssinatura,
        CancellationToken ct = default);
}
