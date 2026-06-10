namespace Imedto.Backend.Contracts.Auth.Queries.Results;

/// <summary>
/// Estado atual do 2FA do usuário autenticado.
/// Retornado por GET /api/auth/2fa/status.
/// O segredo TOTP nunca é devolvido aqui (R8/CA18).
/// </summary>
public record Status2faDto(bool Ativo);

/// <summary>
/// Resultado do passo 1 de ativação do 2FA.
/// Contém a URI otpauth:// (para gerar QR no front) e o segredo base32 (fallback manual).
/// Exposto APENAS durante o fluxo de ativação — nunca pós-confirmação (CA18).
/// </summary>
public record Iniciar2faAtivacaoDto(string OtpauthUri, string SegredoBase32);

/// <summary>
/// Resultado da confirmação de ativação.
/// Os 10 códigos de recuperação são exibidos UMA ÚNICA VEZ (CA1).
/// </summary>
public record Confirmar2faAtivacaoDto(IReadOnlyList<string> CodigosRecuperacao);
