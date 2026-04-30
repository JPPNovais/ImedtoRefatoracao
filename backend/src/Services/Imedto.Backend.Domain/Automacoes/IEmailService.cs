namespace Imedto.Backend.Domain.Automacoes;

/// <summary>
/// Provedor transacional de e-mail.
/// Implementações: <c>ResendEmailService</c> (produção, API Resend) e
/// <c>NoOpEmailService</c> (dev/teste — apenas loga sem enviar).
///
/// Política LGPD: implementações NÃO devem logar destinatário, assunto ou corpo
/// em logs estruturados; apenas hash do destinatário para correlação quando necessário.
/// </summary>
public interface IEmailService
{
    Task EnviarAsync(
        string para,
        string assunto,
        string corpoHtml,
        string? corpoTexto = null,
        CancellationToken ct = default);

    Task EnviarMultiplosAsync(
        IEnumerable<string> paraLista,
        string assunto,
        string corpoHtml,
        string? corpoTexto = null,
        CancellationToken ct = default);
}
