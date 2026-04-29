using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Notificacoes;

[TestFixture]
public class NotificacaoTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    [Test]
    public void Criar_ComCamposBasicos_NotificacaoCriadaCorretamente()
    {
        var notif = Notificacao.Criar(
            UsuarioId, 1, "Consulta amanhã", "Sua consulta está marcada para amanhã.",
            CategoriaNotificacao.Agenda);

        Assert.That(notif.UsuarioId, Is.EqualTo(UsuarioId));
        Assert.That(notif.Titulo, Is.EqualTo("Consulta amanhã"));
        Assert.That(notif.Lida, Is.False);
        Assert.That(notif.LidaEm, Is.Null);
        Assert.That(notif.EstabelecimentoId, Is.EqualTo(1));
    }

    [Test]
    public void Criar_SemEstabelecimento_NotificacaoGlobal()
    {
        var notif = Notificacao.Criar(
            UsuarioId, null, "Convite pendente", "Você tem um convite pendente.",
            CategoriaNotificacao.Sistema);

        Assert.That(notif.EstabelecimentoId, Is.Null);
    }

    [Test]
    public void Criar_UsuarioIdVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Notificacao.Criar(Guid.Empty, null, "Titulo", "Mensagem", CategoriaNotificacao.Sistema));

        Assert.That(ex.Message, Does.Contain("Usuário destinatário é obrigatório"));
    }

    [Test]
    public void Criar_TituloVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Notificacao.Criar(UsuarioId, null, "  ", "Mensagem", CategoriaNotificacao.Sistema));

        Assert.That(ex.Message, Does.Contain("Título da notificação é obrigatório"));
    }

    [Test]
    public void Criar_MensagemVazia_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Notificacao.Criar(UsuarioId, null, "Titulo", "  ", CategoriaNotificacao.Sistema));

        Assert.That(ex.Message, Does.Contain("Mensagem da notificação é obrigatória"));
    }

    [Test]
    public void MarcarComoLida_NotificacaoNaoLida_MarcaLidaEPopulaLidaEm()
    {
        var notif = Notificacao.Criar(UsuarioId, null, "Titulo", "Mensagem", CategoriaNotificacao.Sistema);
        var antes = DateTime.UtcNow;

        notif.MarcarComoLida();

        Assert.That(notif.Lida, Is.True);
        Assert.That(notif.LidaEm, Is.Not.Null);
        Assert.That(notif.LidaEm!.Value, Is.GreaterThanOrEqualTo(antes));
    }

    [Test]
    public void MarcarComoLida_Idempotente_NaoResetaLidaEmOriginal()
    {
        var notif = Notificacao.Criar(UsuarioId, null, "Titulo", "Mensagem", CategoriaNotificacao.Sistema);
        notif.MarcarComoLida();
        var lidaEmOriginal = notif.LidaEm;

        notif.MarcarComoLida(); // segunda chamada

        Assert.That(notif.Lida, Is.True);
        Assert.That(notif.LidaEm, Is.EqualTo(lidaEmOriginal)); // não muda
    }

    [Test]
    public void MarcarComoLida_TerceiraChamada_NaoLancaExcecao()
    {
        var notif = Notificacao.Criar(UsuarioId, null, "Titulo", "Mensagem", CategoriaNotificacao.Sistema);

        Assert.DoesNotThrow(() =>
        {
            notif.MarcarComoLida();
            notif.MarcarComoLida();
            notif.MarcarComoLida();
        });
    }

    [Test]
    public void Criar_TituloComEspacos_AplicaTrim()
    {
        var notif = Notificacao.Criar(UsuarioId, null, "  Meu Titulo  ", "Mensagem", CategoriaNotificacao.Sistema);

        Assert.That(notif.Titulo, Is.EqualTo("Meu Titulo"));
    }

    [Test]
    public void Criar_EstabelecimentoIdZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Notificacao.Criar(UsuarioId, 0, "Titulo", "Mensagem", CategoriaNotificacao.Sistema));

        Assert.That(ex.Message, Does.Contain("Estabelecimento inválido"));
    }
}
