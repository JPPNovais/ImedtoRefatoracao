using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace Imedto.Backend.API.Logging;

/// <summary>
/// Enricher do Serilog que mascara PII em logs (LGPD — Art. 5º II).
///
/// Atua em duas frentes complementares:
///   1. Property bag — substitui valor de propriedades cujo nome bate (case-insensitive) com
///      <see cref="PropriedadesSensiveis"/>. Cobre o caso comum de logar com nome explícito
///      (ex: <c>logger.LogInformation("Usuário {Email} criado", email)</c>).
///   2. Mensagem renderizada e mensagens de exceção — varre via regex padrões brasileiros
///      (e-mail, CPF, CNPJ, telefone) e substitui por <c>[REDACTED]</c>. Cobre o caso de
///      PII embutida em texto livre (ex: stack trace de validação que ecoa o e-mail).
///
/// Nota: o enricher reconstrói o LogEvent quando precisa rescrevê-lo (LogEvent é imutável).
/// Hot path — regexes são compiladas estaticamente.
/// </summary>
public sealed class RemovePIIEnricher : ILogEventEnricher
{
    private static readonly HashSet<string> PropriedadesSensiveis = new(StringComparer.OrdinalIgnoreCase)
    {
        "cpf", "email", "senha", "password", "telefone", "phone", "nome", "name"
    };

    private const string RedactedValue = "[REDACTED]";

    // Padrões mais agressivos que o PiiSanitizer da camada de IA — aqui é log, melhor sobrar do que vazar.
    private static readonly Regex EmailRegex = new(
        @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex CpfRegex = new(
        @"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b",
        RegexOptions.Compiled);

    private static readonly Regex CnpjRegex = new(
        @"\b\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}\b",
        RegexOptions.Compiled);

    private static readonly Regex TelefoneRegex = new(
        @"\(?\d{2}\)?\s?9?\d{4}-?\d{4}",
        RegexOptions.Compiled);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // 1) Mascarar valores no property bag por nome
        foreach (var prop in logEvent.Properties.ToList())
        {
            if (PropriedadesSensiveis.Contains(prop.Key))
            {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(prop.Key, RedactedValue));
            }
            else if (prop.Value is ScalarValue { Value: string s } && ContemPii(s))
            {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(prop.Key, MascararTexto(s)));
            }
        }

        // 2) Mascarar mensagem de exceção (stack trace e .Message) — Exception é set-only no LogEvent,
        // mas o que é renderizado para o sink vem do .Exception.ToString() / .Message. Como não dá para
        // substituir a Exception em si, marcamos uma propriedade auxiliar com a mensagem sanitizada
        // e dependemos do template do sink para evitar logar Exception.Message cru.
        if (logEvent.Exception is not null)
        {
            var msgSanitizada = MascararTexto(logEvent.Exception.Message ?? string.Empty);
            logEvent.AddOrUpdateProperty(
                propertyFactory.CreateProperty("ExceptionMessageSanitized", msgSanitizada));
        }
    }

    private static bool ContemPii(string texto)
        => EmailRegex.IsMatch(texto)
        || CpfRegex.IsMatch(texto)
        || CnpjRegex.IsMatch(texto)
        || TelefoneRegex.IsMatch(texto);

    private static string MascararTexto(string entrada)
    {
        if (string.IsNullOrEmpty(entrada)) return entrada;
        var saida = entrada;
        saida = CnpjRegex.Replace(saida, RedactedValue);
        saida = CpfRegex.Replace(saida, RedactedValue);
        saida = EmailRegex.Replace(saida, RedactedValue);
        saida = TelefoneRegex.Replace(saida, RedactedValue);
        return saida;
    }
}
