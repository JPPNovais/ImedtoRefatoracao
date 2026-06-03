using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Agendamentos;

/// <summary>
/// CA16 — token de confirmação: 256 bits (32 bytes url-safe), único geração a geração,
/// expiração nunca além de InicioPrevisto.
/// CA18 — ConfirmarPorLinkPublico transitiona Agendado→Confirmado com token válido.
/// CA20 — idempotência: já Confirmado lança AgendamentoJaConfirmadoException (não BusinessException).
/// </summary>
[TestFixture]
public class AgendamentoTokenConfirmacaoTests
{
    private static Agendamento CriarAgendado(DateTime? inicio = null)
    {
        var ini = inicio ?? DateTime.UtcNow.AddDays(3);
        return Agendamento.Criar(
            estabelecimentoId: 1,
            pacienteId: 1,
            profissionalUsuarioId: Guid.NewGuid(),
            criadoPorUsuarioId: Guid.NewGuid(),
            inicioPrevisto: ini,
            fimPrevisto: ini.AddHours(1),
            tipoServico: "Consulta",
            observacoes: null);
    }

    // ── CA16: token 256 bits / url-safe / sem padding ─────────────────────

    [Test]
    public void GerarTokenConfirmacao_TokenNaoNulo_EComprimentoCorreto()
    {
        var a = CriarAgendado();
        a.GerarTokenConfirmacao();

        Assert.That(a.TokenConfirmacao, Is.Not.Null.And.Not.Empty);
        // 32 bytes em base64url sem padding = ceil(32*8/6) = 43 caracteres.
        Assert.That(a.TokenConfirmacao!.Length, Is.EqualTo(43),
            "Token 256 bits (32 bytes) deve ter 43 caracteres em base64url sem padding.");
    }

    [Test]
    public void GerarTokenConfirmacao_TokenUrlSafe_SemPaddingNemCaracteresProibidos()
    {
        var a = CriarAgendado();
        a.GerarTokenConfirmacao();

        Assert.That(a.TokenConfirmacao, Does.Not.Contain("+"));
        Assert.That(a.TokenConfirmacao, Does.Not.Contain("/"));
        Assert.That(a.TokenConfirmacao, Does.Not.Contain("="));
    }

    [Test]
    public void GerarTokenConfirmacao_DuasChamadas_TokensDiferentes()
    {
        // CA16: cada geração produz token único.
        var a = CriarAgendado();
        a.GerarTokenConfirmacao();
        var token1 = a.TokenConfirmacao;

        a.GerarTokenConfirmacao();
        var token2 = a.TokenConfirmacao;

        Assert.That(token1, Is.Not.EqualTo(token2));
    }

    [Test]
    public void GerarTokenConfirmacao_ExpiraNuncaAlemDeInicioPrevisto()
    {
        // CA16: expiraEm nunca deve ultrapassar InicioPrevisto.
        var inicio = DateTime.UtcNow.AddHours(2);
        var a = CriarAgendado(inicio);
        a.GerarTokenConfirmacao(TimeSpan.FromDays(7));

        Assert.That(a.TokenConfirmacaoExpiraEm, Is.Not.Null);
        Assert.That(a.TokenConfirmacaoExpiraEm!.Value, Is.LessThanOrEqualTo(inicio).Within(TimeSpan.FromSeconds(5)),
            "Expiração deve ser ≤ InicioPrevisto quando TTL excede a diferença.");
    }

    [Test]
    public void GerarTokenConfirmacao_TtlMenorQueInicioPrevisto_ExpiracaoEhAgora_Mais_Ttl()
    {
        // Token expira antes do inicio previsto quando TTL é pequeno.
        var inicio = DateTime.UtcNow.AddDays(30);
        var a = CriarAgendado(inicio);
        var ttl = TimeSpan.FromHours(1);
        a.GerarTokenConfirmacao(ttl);

        var esperado = DateTime.UtcNow.Add(ttl);
        Assert.That(a.TokenConfirmacaoExpiraEm!.Value,
            Is.EqualTo(esperado).Within(TimeSpan.FromSeconds(5)));
    }

    // ── CA18: confirmar presença Agendado→Confirmado ───────────────────────

    [Test]
    public void ConfirmarPorLinkPublico_TokenValidoAgendado_TransicionaParaConfirmado()
    {
        var a = CriarAgendado(DateTime.UtcNow.AddDays(3));
        a.GerarTokenConfirmacao();

        a.ConfirmarPorLinkPublico("1.2.3.4", "Mozilla/5.0");

        Assert.That(a.Status, Is.EqualTo(AgendamentoStatus.Confirmado));
        Assert.That(a.ConfirmadoPorLinkEm, Is.Not.Null);
    }

    [Test]
    public void ConfirmarPorLinkPublico_TokenExpirado_LancaBusinessException()
    {
        var inicio = DateTime.UtcNow.AddDays(3);
        var a = CriarAgendado(inicio);
        a.GerarTokenConfirmacao(TimeSpan.FromMilliseconds(1));
        // Forçar expiração: o TTL é 1ms, mas InicioPrevisto é 3 dias.
        // Vamos usar reflexão para setar TokenConfirmacaoExpiraEm no passado.
        typeof(Agendamento)
            .GetProperty("TokenConfirmacaoExpiraEm")!
            .SetValue(a, DateTime.UtcNow.AddSeconds(-10));

        Assert.Throws<BusinessException>(() =>
            a.ConfirmarPorLinkPublico(null, null));
    }

    [Test]
    public void ConfirmarPorLinkPublico_SemToken_LancaBusinessException()
    {
        var a = CriarAgendado();
        // Sem chamar GerarTokenConfirmacao — TokenConfirmacao é null.
        Assert.Throws<BusinessException>(() =>
            a.ConfirmarPorLinkPublico(null, null));
    }

    // ── CA20: idempotência — já Confirmado lança AgendamentoJaConfirmadoException ──

    [Test]
    public void ConfirmarPorLinkPublico_JaConfirmado_LancaAgendamentoJaConfirmadoException()
    {
        var a = CriarAgendado(DateTime.UtcNow.AddDays(3));
        a.GerarTokenConfirmacao();
        a.ConfirmarPorLinkPublico(null, null); // primeira confirmação

        // Segunda chamada: idempotência — não é erro fatal, handler trata separadamente.
        Assert.Throws<AgendamentoJaConfirmadoException>(() =>
            a.ConfirmarPorLinkPublico(null, null));
    }

    [Test]
    public void ConfirmarPorLinkPublico_Cancelado_LancaBusinessException()
    {
        // CA19: cancelado → mensagem genérica (BusinessException, não JaConfirmado).
        var a = CriarAgendado(DateTime.UtcNow.AddDays(3));
        a.GerarTokenConfirmacao();
        a.Cancelar("motivo teste");

        Assert.Throws<BusinessException>(() =>
            a.ConfirmarPorLinkPublico(null, null));
    }
}
