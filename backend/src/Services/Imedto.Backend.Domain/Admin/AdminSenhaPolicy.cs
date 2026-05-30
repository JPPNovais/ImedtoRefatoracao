using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Política de senha para admins globais.
///
/// Dev: ≥ 6 chars (produtividade local).
/// Prod: ≥ 10 chars + maiúscula + minúscula + número + especial.
///
/// Detectado via flag injetado pelo caller (normalmente <c>IWebHostEnvironment.IsDevelopment()</c>).
/// </summary>
public static class AdminSenhaPolicy
{
    private const int MinimoDev = 6;
    private const int MinimoProd = 10;
    private static readonly char[] Especiais = "!@#$%^&*()_+-=[]{}|;':\",./<>?".ToCharArray();

    /// <summary>
    /// Valida a senha segundo o ambiente. Lança <see cref="BusinessException"/> se inválida.
    /// </summary>
    public static void Validar(string senha, bool isDevelopment)
    {
        if (string.IsNullOrEmpty(senha))
            throw new BusinessException("Senha é obrigatória.");

        if (isDevelopment)
        {
            if (senha.Length < MinimoDev)
                throw new BusinessException($"Senha deve ter no mínimo {MinimoDev} caracteres.");
            return;
        }

        // Produção — política completa.
        if (senha.Length < MinimoProd)
            throw new BusinessException(
                "Senha deve ter no mínimo 10 caracteres, incluindo maiúscula, minúscula, número e caractere especial.");

        if (!senha.Any(char.IsUpper))
            throw new BusinessException(
                "Senha deve ter no mínimo 10 caracteres, incluindo maiúscula, minúscula, número e caractere especial.");

        if (!senha.Any(char.IsLower))
            throw new BusinessException(
                "Senha deve ter no mínimo 10 caracteres, incluindo maiúscula, minúscula, número e caractere especial.");

        if (!senha.Any(char.IsDigit))
            throw new BusinessException(
                "Senha deve ter no mínimo 10 caracteres, incluindo maiúscula, minúscula, número e caractere especial.");

        if (!senha.Any(c => Especiais.Contains(c)))
            throw new BusinessException(
                "Senha deve ter no mínimo 10 caracteres, incluindo maiúscula, minúscula, número e caractere especial.");
    }

    /// <summary>
    /// Gera senha aleatória que satisfaz a política de produção (20 chars).
    /// Usada pelo CLI <c>seed-admin</c> para gerar senha temporária.
    /// </summary>
    public static string GerarSenhaTemporaria()
    {
        const string maiusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string minusculas = "abcdefghjkmnpqrstuvwxyz";
        const string numeros = "23456789";
        const string especiais = "!@#$%&*";
        const string todos = maiusculas + minusculas + numeros + especiais;

        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[20];
        rng.GetBytes(bytes);

        // Garante ao menos 1 de cada categoria nos primeiros 4 chars.
        var chars = new char[20];
        chars[0] = maiusculas[bytes[0] % maiusculas.Length];
        chars[1] = minusculas[bytes[1] % minusculas.Length];
        chars[2] = numeros[bytes[2] % numeros.Length];
        chars[3] = especiais[bytes[3] % especiais.Length];

        for (var i = 4; i < 20; i++)
            chars[i] = todos[bytes[i] % todos.Length];

        // Embaralha Fisher-Yates para não ter padrão fixo nos primeiros chars.
        var random = new System.Random();
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
