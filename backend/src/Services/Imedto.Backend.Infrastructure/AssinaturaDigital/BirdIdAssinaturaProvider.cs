using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Imedto.Backend.Domain.AssinaturaDigital;
using Imedto.Backend.Domain.Receitas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Imedto.Backend.Infrastructure.AssinaturaDigital;

/// <summary>
/// Opções de configuração do provedor BirdID (Soluti).
/// Lidas de <c>AssinaturaDigital:BirdId</c> no appsettings.
/// </summary>
public class BirdIdOptions
{
    public const string Section = "AssinaturaDigital:BirdId";

    /// <summary>Client ID da aplicação no BirdID.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client Secret (SSM SecureString).</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>URL base da API BirdID. Dev: https://apihom.birdid.com.br</summary>
    public string BaseUrl { get; set; } = "https://apihom.birdid.com.br";

    /// <summary>Secret HMAC para validar callbacks do webhook.</summary>
    public string WebhookSecret { get; set; } = string.Empty;
}

/// <summary>
/// Implementação stub/skeleton do provedor BirdID.
///
/// Estado atual: a integração OAuth2 + PAdES BirdID aguarda liberação do canal
/// comercial pela Soluti. Enquanto isso:
/// - <see cref="DispararAssinaturaAsync"/> loga a tentativa e retorna resultado
///   indicando modo de homologação (sem chamar a API real).
/// - <see cref="ValidarCallbackAsync"/> implementa validação HMAC-SHA256 REAL
///   usando o <see cref="BirdIdOptions.WebhookSecret"/> — segurança do webhook
///   está funcional independentemente do resto.
///
/// Quando o canal for liberado, substituir o corpo de DispararAssinaturaAsync
/// pelo fluxo OAuth2 PKCE + POST para a API de assinatura PAdES. O contrato da
/// interface permanece inalterado.
/// </summary>
public class BirdIdAssinaturaProvider : IAssinaturaDigitalProvider
{
    private readonly BirdIdOptions _options;
    private readonly ILogger<BirdIdAssinaturaProvider> _logger;

    public BirdIdAssinaturaProvider(
        IOptions<BirdIdOptions> options,
        ILogger<BirdIdAssinaturaProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<DisparoAssinaturaResult> DispararAssinaturaAsync(
        Receita receita,
        Guid medicoId,
        string refreshTokenDecifrado,
        CancellationToken ct = default)
    {
        // Stub: loga a tentativa e retorna resultado de homologação.
        // O fluxo real: obter access_token via refresh_token + enviar PDF para assinatura PAdES.
        _logger.LogInformation(
            "[BirdID] Disparo em modo homologação para receita {ReceitaId} (médico {MedicoId}). " +
            "Canal BirdID ainda não liberado — aguardando aprovação Soluti.",
            receita.Id, medicoId);

        return Task.FromResult(new DisparoAssinaturaResult(
            Sucesso: true,
            ModoHomologacao: true));
    }

    public Task<ValidacaoCallbackResult> ValidarCallbackAsync(
        string payload,
        string headerAssinatura,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            // Sem secret configurado (dev local sem SSM): aceita qualquer callback.
            // NUNCA em produção — a chave DEVE estar no SSM.
            _logger.LogWarning(
                "[BirdID] WebhookSecret não configurado — aceitando callback sem validação HMAC. " +
                "Configurar AssinaturaDigital:BirdId:WebhookSecret em produção.");
            return Task.FromResult(new ValidacaoCallbackResult(
                AssinaturaValida: true,
                Sucesso: false, // Sem PDF real no stub.
                PdfBase64: null));
        }

        // Validação HMAC-SHA256 real.
        // Formato BirdID: header = "sha256=<hex>" — mesmo padrão GitHub webhooks.
        var hmacValido = ValidarHmacSha256(payload, headerAssinatura, _options.WebhookSecret);
        if (!hmacValido)
        {
            return Task.FromResult(new ValidacaoCallbackResult(
                AssinaturaValida: false,
                Sucesso: false,
                MensagemErro: "HMAC inválido."));
        }

        // Stub: HMAC válido mas sem PDF real (canal não liberado).
        // Quando o canal for liberado: parsear o payload, extrair o PDF base64 e retornar Sucesso=true.
        string? pdfBase64 = ExtrairPdfDoPayload(payload);
        bool sucesso = pdfBase64 is not null;

        return Task.FromResult(new ValidacaoCallbackResult(
            AssinaturaValida: true,
            Sucesso: sucesso,
            PdfBase64: pdfBase64));
    }

    private static bool ValidarHmacSha256(string payload, string header, string secret)
    {
        // header esperado: "sha256=<lowercase-hex>"
        if (!header.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            return false;

        var expectedHex = header["sha256=".Length..];
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var computed = hmac.ComputeHash(payloadBytes);
        var computedHex = Convert.ToHexString(computed).ToLowerInvariant();

        // Comparação constante-em-tempo para evitar timing attack.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex),
            Encoding.UTF8.GetBytes(expectedHex.ToLowerInvariant()));
    }

    /// <summary>
    /// Tenta extrair o PDF base64 do payload JSON do BirdID.
    /// Formato esperado (a ser confirmado com docs reais): <c>{ "pdf": "base64..." }</c>.
    /// Retorna null se não encontrar — callback sem PDF trata como falha.
    /// </summary>
    private static string? ExtrairPdfDoPayload(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("pdf", out var pdfProp))
                return pdfProp.GetString();
        }
        catch
        {
            // Payload malformado — tratado pelo caller como falha.
        }
        return null;
    }
}
