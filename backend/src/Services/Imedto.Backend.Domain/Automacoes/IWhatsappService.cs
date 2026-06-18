namespace Imedto.Backend.Domain.Automacoes;

/// <summary>
/// Provedor de mensagens WhatsApp via template aprovado.
/// Implementações: <c>MetaWhatsappService</c> (produção, Meta Cloud API) e
/// <c>NoOpWhatsappService</c> (dev/teste — apenas loga sem enviar).
///
/// Política LGPD: implementações NÃO devem logar o destinatário (telefone),
/// corpo da mensagem ou variáveis do template em logs estruturados;
/// apenas hash SHA-256 truncado do destinatário para correlação quando necessário.
/// </summary>
public interface IWhatsappService
{
    /// <summary>
    /// Envia uma mensagem de template WhatsApp para o destinatário.
    /// </summary>
    /// <param name="para">Número de telefone do destinatário em formato E.164 (ex: +5511999999999).</param>
    /// <param name="variaveis">
    /// Variáveis do corpo do template na ordem declarada no template aprovado pela Meta.
    /// </param>
    /// <param name="ct">Token de cancelamento.</param>
    Task EnviarTemplateAsync(
        string para,
        IReadOnlyList<string> variaveis,
        CancellationToken ct = default);
}
