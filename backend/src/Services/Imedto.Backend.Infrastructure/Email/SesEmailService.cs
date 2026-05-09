using System.Security.Cryptography;
using System.Text;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Automacoes;

namespace Imedto.Backend.Infrastructure.Email;

/// <summary>
/// Implementação alternativa do <see cref="IEmailService"/> usando AWS SES v2.
/// Vantagens vs. Resend:
/// - Free tier 62.000 e-mails/mês quando enviado de EC2 (nosso caso).
/// - Preço em escala: US$ 0,10/1.000 (4× mais barato).
/// - Residência sa-east-1 (LGPD friendly).
///
/// Credenciais: IAM role da EC2 (em prod) ou ~/.aws/credentials (em dev).
/// Sender domain (imedto.com) precisa estar verificado no SES e fora do sandbox
/// para enviar em produção real.
///
/// Política LGPD: idêntica à do <see cref="ResendEmailService"/> — logs nunca incluem
/// destinatário, assunto ou corpo. Apenas hash SHA-256 truncado para correlação.
/// </summary>
public class SesEmailService : IEmailService
{
    private readonly IAmazonSimpleEmailServiceV2 _ses;
    private readonly ILogger<SesEmailService> _logger;
    private readonly string _from;
    private readonly string _configurationSet;

    public SesEmailService(
        IAmazonSimpleEmailServiceV2 ses,
        IConfiguration config,
        ILogger<SesEmailService> logger)
    {
        _ses = ses;
        _logger = logger;
        _from = config["Email:From"] ?? "Imedto <noreply@imedto.com>";
        _configurationSet = config["Email:Ses:ConfigurationSet"];
    }

    public Task EnviarAsync(
        string para,
        string assunto,
        string corpoHtml,
        string? corpoTexto = null,
        CancellationToken ct = default)
        => EnviarInternoAsync(new[] { para }, assunto, corpoHtml, corpoTexto, ct);

    public Task EnviarMultiplosAsync(
        IEnumerable<string> paraLista,
        string assunto,
        string corpoHtml,
        string? corpoTexto = null,
        CancellationToken ct = default)
    {
        var destinatarios = paraLista?.Where(e => !string.IsNullOrWhiteSpace(e)).ToArray()
            ?? Array.Empty<string>();
        if (destinatarios.Length == 0) return Task.CompletedTask;
        return EnviarInternoAsync(destinatarios, assunto, corpoHtml, corpoTexto, ct);
    }

    private async Task EnviarInternoAsync(
        string[] destinatarios,
        string assunto,
        string corpoHtml,
        string? corpoTexto,
        CancellationToken ct)
    {
        var hashCorrelacao = HashCorrelacao(destinatarios);

        var request = new SendEmailRequest
        {
            FromEmailAddress = _from,
            Destination = new Destination { ToAddresses = destinatarios.ToList() },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = assunto, Charset = "UTF-8" },
                    Body = new Body
                    {
                        Html = new Content { Data = corpoHtml, Charset = "UTF-8" },
                        Text = string.IsNullOrEmpty(corpoTexto)
                            ? null
                            : new Content { Data = corpoTexto, Charset = "UTF-8" }
                    }
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(_configurationSet))
            request.ConfigurationSetName = _configurationSet;

        try
        {
            var response = await _ses.SendEmailAsync(request, ct);
            _logger.LogInformation(
                "SES enviou e-mail (MessageId {MessageId}, hash {Hash}).",
                response.MessageId, hashCorrelacao);
        }
        catch (MessageRejectedException ex)
        {
            // E-mail rejeitado por política (sandbox: destinatário não verificado, etc).
            _logger.LogError(ex,
                "SES rejeitou e-mail (hash {Hash}): {Mensagem}",
                hashCorrelacao, ex.Message);
        }
        catch (MailFromDomainNotVerifiedException ex)
        {
            _logger.LogError(ex,
                "SES: domínio do From não verificado (hash {Hash}).", hashCorrelacao);
        }
        catch (Exception ex)
        {
            // Qualquer outro erro (rede, throttling, etc) — não bloqueia o caller.
            _logger.LogError(ex,
                "Falha ao enviar e-mail via SES (hash {Hash}).", hashCorrelacao);
        }
    }

    private static string HashCorrelacao(IEnumerable<string> destinatarios)
    {
        var concat = string.Join(';', destinatarios);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(concat)))[..16];
    }
}
