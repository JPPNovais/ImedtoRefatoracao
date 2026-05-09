using System.Text;

namespace Imedto.Backend.Infrastructure.Email;

/// <summary>
/// Templates HTML transacionais do Imedto. Render mantém o visual do produto
/// (header roxo #442B97, footer fixo, info-box) — espelha o que era enviado pela
/// stack legada (Supabase Edge Functions).
///
/// Política LGPD:
///   * Conteúdo nunca embute CPF, telefone ou outros campos sensíveis.
///   * Para variáveis dinâmicas (link, e-mail, nome do estabelecimento) sempre passar
///     pelos métodos públicos — o HTML interno é sanitizado via <see cref="HtmlEscape"/>.
///   * Não logar o HTML produzido (pode conter URL com token).
/// </summary>
public static class EmailTemplates
{
    /// <summary>Render de e-mail de confirmação de cadastro.</summary>
    public static string Confirmacao(string link)
    {
        var conteudo = $$"""
            <h1>Confirme seu cadastro no Imedto</h1>
            <p>Olá,</p>
            <p>Obrigado por se cadastrar no <strong>Imedto</strong>! Estamos felizes em tê-lo conosco.</p>
            <p>Para ativar sua conta e começar a usar nossa plataforma, precisamos confirmar seu endereço de e-mail.</p>
            <div class="info-box">
              <p><strong>Por que precisamos confirmar seu e-mail?</strong></p>
              <p>A confirmação do e-mail garante a segurança da sua conta e nos permite enviar notificações importantes sobre seus agendamentos e prontuários.</p>
            </div>
            <p>Clique no botão abaixo para confirmar seu e-mail:</p>
            <p style="text-align:center;"><a href="{{HtmlEscape(link)}}" class="button">Confirmar e-mail</a></p>
            <div class="divider"></div>
            <p style="font-size:14px;color:#737373;">
              <strong>O link expira em 24 horas.</strong> Se você não confirmar seu e-mail dentro deste período, será necessário solicitar um novo link de confirmação.
            </p>
            <p style="font-size:14px;color:#737373;">
              Se o botão não funcionar, copie e cole este link no seu navegador:<br>
              <span style="word-break:break-all;color:#442B97;">{{HtmlEscape(link)}}</span>
            </p>
            <p style="font-size:14px;color:#737373;">
              Se você não criou uma conta no Imedto, você pode ignorar este e-mail com segurança.
            </p>
            """;
        return Wrap("Confirme seu cadastro no Imedto", conteudo);
    }

    /// <summary>Render de e-mail de redefinição de senha.</summary>
    public static string ResetSenha(string link)
    {
        var conteudo = $$"""
            <h1>Redefinir sua senha</h1>
            <p>Olá,</p>
            <p>Recebemos uma solicitação para redefinir a senha da sua conta no <strong>Imedto</strong>.</p>
            <div class="info-box">
              <p><strong>Não solicitou a redefinição?</strong></p>
              <p>Se você não solicitou a redefinição de senha, pode ignorar este e-mail com segurança. Sua senha permanecerá inalterada.</p>
            </div>
            <p>Para criar uma nova senha, clique no botão abaixo:</p>
            <p style="text-align:center;"><a href="{{HtmlEscape(link)}}" class="button">Redefinir senha</a></p>
            <div class="divider"></div>
            <p style="font-size:14px;color:#737373;">
              <strong>O link expira em 1 hora.</strong> Por motivos de segurança, este link só pode ser usado uma vez e expira em 60 minutos.
            </p>
            <p style="font-size:14px;color:#737373;">
              Se o botão não funcionar, copie e cole este link no seu navegador:<br>
              <span style="word-break:break-all;color:#442B97;">{{HtmlEscape(link)}}</span>
            </p>
            <p style="font-size:14px;color:#737373;">
              <strong>Dica de segurança:</strong> use uma senha forte com pelo menos 8 caracteres, combinando letras maiúsculas, minúsculas, números e símbolos.
            </p>
            """;
        return Wrap("Redefinir sua senha", conteudo);
    }

    /// <summary>
    /// Render de convite para vincular-se a um estabelecimento. Mensagem genérica:
    /// não inclui o nome do estabelecimento (LGPD/UX — detalhes ficam na tela
    /// "Meus convites" depois do login).
    /// </summary>
    public static string ConviteVinculo(string appUrl)
    {
        var url = HtmlEscape(appUrl.TrimEnd('/'));
        var conteudo = $$"""
            <h1>Você foi convidado para o Imedto</h1>
            <p>Olá,</p>
            <p>Você recebeu um convite para se vincular a um estabelecimento no <strong>Imedto</strong>.</p>
            <div class="info-box">
              <p><strong>O que é o Imedto?</strong></p>
              <p>O Imedto é um sistema completo de gestão em saúde — agenda, prontuário eletrônico, financeiro e equipe num só lugar, com segurança e conformidade LGPD.</p>
            </div>
            <p>Para revisar e aceitar o convite:</p>
            <ol style="color:#737373;margin-left:20px;margin-bottom:20px;">
              <li>Acesse a plataforma com o e-mail que recebeu este convite.</li>
              <li>Se ainda não tem uma conta, crie uma usando este mesmo e-mail.</li>
              <li>Abra a aba <strong>Meus convites</strong> e aceite a solicitação.</li>
            </ol>
            <p style="text-align:center;"><a href="{{url}}/meus-convites" class="button">Acessar Imedto</a></p>
            <div class="divider"></div>
            <p style="font-size:14px;color:#737373;">
              Se você não esperava receber este convite, pode simplesmente ignorar este e-mail.
            </p>
            """;
        return Wrap("Convite para o Imedto", conteudo);
    }

    /// <summary>
    /// Render de notificação ao dono de estabelecimento — um profissional solicitou
    /// vínculo. Sem PII (nome/e-mail do solicitante) — detalhes ficam na tela
    /// "Solicitações recebidas".
    /// </summary>
    public static string SolicitacaoVinculoRecebida(string appUrl)
    {
        var url = HtmlEscape(appUrl.TrimEnd('/'));
        var conteudo = $$"""
            <h1>Nova solicitação de vínculo</h1>
            <p>Olá,</p>
            <p>Um profissional solicitou acesso ao seu estabelecimento no <strong>Imedto</strong>.</p>
            <div class="info-box">
              <p><strong>O que fazer agora?</strong></p>
              <p>Acesse a aba <strong>Solicitações recebidas</strong> para revisar os dados do profissional e aceitar ou recusar a solicitação.</p>
            </div>
            <p style="text-align:center;"><a href="{{url}}/solicitacoes-vinculo/recebidas" class="button">Revisar solicitação</a></p>
            <div class="divider"></div>
            <p style="font-size:14px;color:#737373;">
              Você está recebendo este e-mail porque é proprietário de um estabelecimento no Imedto.
            </p>
            """;
        return Wrap("Nova solicitação de vínculo", conteudo);
    }

    private static string Wrap(string titulo, string conteudo)
    {
        return $$"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>{{HtmlEscape(titulo)}}</title>
              <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body { font-family: 'Nunito', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #3D3D3D; background-color: #F5F5FA; }
                .email-container { max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; }
                .header { background-color: #442B97; padding: 36px 30px; text-align: center; }
                .logo { font-size: 28px; font-weight: 800; color: #ffffff; letter-spacing: 1px; }
                .content { padding: 40px 30px; }
                .content h1 { font-size: 22px; color: #3D3D3D; margin-bottom: 20px; font-weight: 700; }
                .content p { font-size: 15px; color: #737373; margin-bottom: 16px; line-height: 1.8; }
                .button { display: inline-block; background-color: #442B97; color: #ffffff !important; text-decoration: none; padding: 14px 32px; border-radius: 8px; font-weight: 700; font-size: 15px; margin: 20px 0; }
                .info-box { background-color: #EFECF9; border-left: 4px solid #442B97; padding: 16px 20px; margin: 24px 0; border-radius: 4px; }
                .info-box p { margin-bottom: 8px; font-size: 14px; color: #3D3D3D; }
                .info-box p:last-child { margin-bottom: 0; }
                .footer { background-color: #F5F5FA; padding: 28px 30px; text-align: center; border-top: 1px solid #E4E4E8; }
                .footer p { font-size: 13px; color: #737373; margin-bottom: 6px; }
                .footer a { color: #442B97; text-decoration: none; }
                .divider { height: 1px; background-color: #E4E4E8; margin: 28px 0; }
                @media only screen and (max-width: 600px) {
                  .content { padding: 30px 20px; }
                  .header { padding: 28px 20px; }
                  .content h1 { font-size: 20px; }
                  .button { display: block; text-align: center; }
                }
              </style>
            </head>
            <body>
              <div class="email-container">
                <div class="header"><div class="logo">Imedto</div></div>
                <div class="content">
                  {{conteudo}}
                </div>
                <div class="footer">
                  <p><strong>Imedto</strong> — Sistema de gestão em saúde</p>
                  <p>Este é um e-mail automático. Por favor, não responda.</p>
                  <p>Se tiver dúvidas, entre em contato com o suporte.</p>
                </div>
              </div>
            </body>
            </html>
            """;
    }

    private static string HtmlEscape(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return string.Empty;
        var sb = new StringBuilder(valor.Length + 16);
        foreach (var c in valor)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\'': sb.Append("&#39;"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
