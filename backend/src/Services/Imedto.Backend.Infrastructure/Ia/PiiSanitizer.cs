using System.Text.RegularExpressions;

namespace Imedto.Backend.Infrastructure.Ia;

/// <summary>
/// Redação string-level de PII brasileira antes de enviar conteúdo para a IA.
///
/// Estratégia: aplicado sobre o JSON serializado do request (varredura ampla) — qualquer
/// campo novo que venha a entrar em <see cref="Imedto.Backend.Domain.Ia.SugestaoSecaoProntuarioRequest"/>
/// no futuro é coberto por padrão, evitando o risco de "esqueceram de sanitizar o campo X".
///
/// Trade-off conhecido: regex string-level pode ser tanto sub-inclusiva (PII em formatos não
/// canônicos passam) quanto super-inclusiva (sequências numéricas legítimas batem com padrão
/// de telefone). É melhor que enviar o cru, mas NÃO é substituto da minimização semântica
/// (campo a campo, no momento de montar o request) — esta deve continuar valendo na fronteira.
/// </summary>
public static class PiiSanitizer
{
    // Regexes compiladas — execução em hot path (toda chamada de IA).
    // Ordem importa: padrões mais longos primeiro para não serem comidos por padrões mais curtos.

    // CNPJ alfanumérico (IN RFB 2.229/2024): 12 primeiras posições [A-Z0-9], 2 DVs numéricos.
    // Casa tanto o formato numérico clássico quanto o novo alfanumérico, com ou sem máscara.
    private static readonly Regex CnpjRegex = new(
        @"\b[A-Z0-9]{2}\.?[A-Z0-9]{3}\.?[A-Z0-9]{3}/?[A-Z0-9]{4}-?\d{2}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Celular: DDD + dígito 9 obrigatório (regra ANATEL pós-2014) + 8 dígitos.
    // Casa "(11) 99999-8888" e "11999998888" mas NÃO casa "12345678909" (CPF cru sem 9 no 3º dígito).
    private static readonly Regex TelefoneCelularRegex = new(
        @"\(?\d{2}\)?[\s\-]?9\d{4}-?\d{4}",
        RegexOptions.Compiled);

    // Fixo: exige máscara (parênteses no DDD OU hífen entre os blocos 4+4) — sem
    // máscara seria indistinguível de outros números de 10 dígitos (RG, ID, etc.).
    private static readonly Regex TelefoneFixoRegex = new(
        @"\(\d{2}\)\s?\d{4}-?\d{4}|\b\d{2}\s\d{4}-\d{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex CpfRegex = new(
        @"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b",
        RegexOptions.Compiled);

    private static readonly Regex EmailRegex = new(
        @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex CepRegex = new(
        @"\b\d{5}-?\d{3}\b",
        RegexOptions.Compiled);

    private static readonly Regex RgRegex = new(
        @"\b\d{1,2}\.?\d{3}\.?\d{3}-?[\dxX]\b",
        RegexOptions.Compiled);

    /// <summary>
    /// Substitui PII por placeholders. Recebe e devolve string — aplicar antes de mandar
    /// o conteúdo para a IA E antes de gerar o hash de cache/audit (assim o cache também
    /// é indexado pelo conteúdo já sanitizado, sem nunca tocar dados crus).
    /// </summary>
    public static string Sanitize(string entrada)
    {
        if (string.IsNullOrEmpty(entrada))
            return entrada;

        var saida = entrada;

        // Ordem importa: CNPJ (14 dígitos, mais distintivo) > telefone (estrito, evita
        // colidir com CPF cru) > CPF > demais. Telefone vem ANTES de CPF para que
        // celulares de 11 dígitos (com 9 fixo no 3º dígito) sejam corretamente
        // identificados como [TELEFONE_REDACTED] e não como [CPF_REDACTED].
        saida = CnpjRegex.Replace(saida, "[CNPJ_REDACTED]");
        saida = TelefoneCelularRegex.Replace(saida, "[TELEFONE_REDACTED]");
        saida = TelefoneFixoRegex.Replace(saida, "[TELEFONE_REDACTED]");
        saida = CpfRegex.Replace(saida, "[CPF_REDACTED]");
        saida = EmailRegex.Replace(saida, "[EMAIL_REDACTED]");
        saida = CepRegex.Replace(saida, "[CEP_REDACTED]");
        saida = RgRegex.Replace(saida, "[RG_REDACTED]");

        return saida;
    }
}
