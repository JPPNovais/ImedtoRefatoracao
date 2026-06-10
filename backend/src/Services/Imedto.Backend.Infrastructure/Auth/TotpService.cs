using System.Security.Cryptography;
using System.Text;

namespace Imedto.Backend.Infrastructure.Auth;

/// <summary>
/// Implementação RFC 6238 (TOTP): HMAC-SHA1, 6 dígitos, period 30s, janela ±1 step.
/// Sem dependência de pacote externo — o algoritmo cabe em ~50 linhas e é totalmente
/// determinístico dado um segredo base32 e um timestamp UTC.
///
/// Parâmetros fixos deliberados: SHA1+30s+6 dígitos são os defaults universais dos
/// apps autenticadores (Google Authenticator, Authy, 1Password). Expor configuração
/// criaria incompatibilidade silenciosa com apps já registrados.
/// </summary>
public static class TotpService
{
    private const int DigitCount  = 6;
    private const int StepSeconds = 30;
    private const int Modulo      = 1_000_000; // 10^DigitCount

    /// <summary>
    /// Gera o código TOTP atual para o segredo base32 informado.
    /// Útil em testes — o código deve ser validado via <see cref="Validar"/>.
    /// </summary>
    public static string GerarCodigo(string segredoBase32, DateTime? agora = null)
    {
        var t = StepAtual(agora ?? DateTime.UtcNow);
        return Calcular(DecodeBase32(segredoBase32), t).ToString("D6");
    }

    /// <summary>
    /// Valida o código TOTP contra o segredo base32 com janela ±1 step (R8 / CA3).
    /// Retorna true se o código bater com o step atual, anterior ou seguinte.
    /// </summary>
    public static bool Validar(string segredoBase32, string codigo, DateTime? agora = null)
    {
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != DigitCount)
            return false;

        if (!int.TryParse(codigo, out var inputNum))
            return false;

        var key  = DecodeBase32(segredoBase32);
        var step = StepAtual(agora ?? DateTime.UtcNow);

        // Janela ±1 step (CA6, §8 clock skew)
        for (var delta = -1; delta <= 1; delta++)
        {
            if (Calcular(key, step + delta) == inputNum)
                return true;
        }
        return false;
    }

    /// <summary>Gera um segredo TOTP aleatório seguro (20 bytes = 160 bits) em base32.</summary>
    public static string GerarSegredoBase32()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return EncodeBase32(bytes);
    }

    /// <summary>
    /// Monta a URI otpauth:// compatível com Google Authenticator / Authy / 1Password.
    /// Formato: <c>otpauth://totp/Imedto:{email}?secret={base32}&amp;issuer=Imedto&amp;algorithm=SHA1&amp;digits=6&amp;period=30</c>
    /// </summary>
    public static string MontarOtpauthUri(string email, string segredoBase32)
    {
        var label = Uri.EscapeDataString($"Imedto:{email}");
        return $"otpauth://totp/{label}?secret={segredoBase32}&issuer=Imedto&algorithm=SHA1&digits=6&period=30";
    }

    // ── RFC 4648 Base32 (A-Z, 2-7) ──────────────────────────────────────────

    private static readonly char[] Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    public static string EncodeBase32(byte[] data)
    {
        var sb = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = 0, bitsLeft = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                sb.Append(Base32Chars[(buffer >> bitsLeft) & 0x1F]);
            }
        }
        if (bitsLeft > 0)
            sb.Append(Base32Chars[(buffer << (5 - bitsLeft)) & 0x1F]);
        return sb.ToString();
    }

    public static byte[] DecodeBase32(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Segredo base32 inválido.");

        var clean = input.ToUpperInvariant().TrimEnd('=');
        var bytes = new byte[clean.Length * 5 / 8];
        int buffer = 0, bitsLeft = 0, idx = 0;
        foreach (var c in clean)
        {
            var v = Array.IndexOf(Base32Chars, c);
            if (v < 0) throw new ArgumentException($"Caractere base32 inválido: {c}");
            buffer = (buffer << 5) | v;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                bytes[idx++] = (byte)(buffer >> bitsLeft);
            }
        }
        return bytes;
    }

    // ── HOTP/TOTP (RFC 4226 + RFC 6238) ─────────────────────────────────────

    private static long StepAtual(DateTime utcNow)
        => new DateTimeOffset(utcNow, TimeSpan.Zero).ToUnixTimeSeconds() / StepSeconds;

    private static int Calcular(byte[] key, long counter)
    {
        var msg = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(msg);

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(msg);

        var offset = hash[^1] & 0x0F;
        var code = ((hash[offset] & 0x7F) << 24)
                 | ((hash[offset + 1] & 0xFF) << 16)
                 | ((hash[offset + 2] & 0xFF) << 8)
                 | (hash[offset + 3] & 0xFF);

        return code % Modulo;
    }
}
