namespace Imedto.Backend.Infrastructure.Auth;

/// <summary>Templates HTML mínimos para e-mails de auth. Sem PII no corpo.</summary>
internal static class TemplatesEmail
{
    public static string Confirmacao(string link) => $$"""
        <p>Olá,</p>
        <p>Para confirmar seu cadastro no Imedto, clique no link abaixo (válido por 24 horas):</p>
        <p><a href="{{link}}">Confirmar e-mail</a></p>
        <p>Se você não criou esta conta, ignore esta mensagem.</p>
        <p>— Equipe Imedto</p>
        """;

    public static string ResetSenha(string link) => $$"""
        <p>Olá,</p>
        <p>Recebemos uma solicitação de redefinição de senha. Se foi você, clique no link abaixo (válido por 1 hora):</p>
        <p><a href="{{link}}">Redefinir senha</a></p>
        <p>Se você não solicitou, ignore — sua senha continua a mesma.</p>
        <p>— Equipe Imedto</p>
        """;

    public static string Convite(string nomeEstabelecimento, string link) => $$"""
        <p>Olá,</p>
        <p>Você foi convidado(a) a participar do estabelecimento <strong>{{nomeEstabelecimento}}</strong> no Imedto.</p>
        <p>Clique no link abaixo para definir sua senha e aceitar o convite (válido por 7 dias):</p>
        <p><a href="{{link}}">Aceitar convite</a></p>
        <p>— Equipe Imedto</p>
        """;
}
