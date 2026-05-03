using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Vinculos;

[TestFixture]
public class SolicitacaoVinculoTests
{
    private static SolicitacaoVinculo CriarValida() =>
        SolicitacaoVinculo.Solicitar(
            profissionalUsuarioId: Guid.NewGuid(),
            estabelecimentoId: 1L,
            mensagem: "Gostaria de atender nesta clínica.");

    // ----- Solicitar -----

    [Test]
    public void Solicitar_Valida_StatusPendente()
    {
        var s = CriarValida();
        Assert.That(s.Status, Is.EqualTo(StatusSolicitacaoVinculo.Pendente));
        Assert.That(s.RespondidaEm, Is.Null);
        Assert.That(s.CriadaEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void Solicitar_MensagemTrimada_RemoveEspacos()
    {
        var s = SolicitacaoVinculo.Solicitar(Guid.NewGuid(), 1L, "  oi  ");
        Assert.That(s.Mensagem, Is.EqualTo("oi"));
    }

    [Test]
    public void Solicitar_MensagemVazia_PermiteNull()
    {
        var s = SolicitacaoVinculo.Solicitar(Guid.NewGuid(), 1L, "  ");
        Assert.That(s.Mensagem, Is.Null);
    }

    [Test]
    public void Solicitar_ProfissionalEmpty_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            SolicitacaoVinculo.Solicitar(Guid.Empty, 1L, null));
        Assert.That(ex.Message, Does.Contain("Profissional"));
    }

    [Test]
    public void Solicitar_EstabelecimentoZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            SolicitacaoVinculo.Solicitar(Guid.NewGuid(), 0L, null));
        Assert.That(ex.Message, Does.Contain("Estabelecimento"));
    }

    [Test]
    public void Solicitar_MensagemMaiorQue1000_LancaBusinessException()
    {
        var msg = new string('a', 1001);
        var ex = Assert.Throws<BusinessException>(() =>
            SolicitacaoVinculo.Solicitar(Guid.NewGuid(), 1L, msg));
        Assert.That(ex.Message, Does.Contain("1000"));
    }

    // ----- MarcarComoCriada -----

    [Test]
    public void MarcarComoCriada_IdZero_LancaInvalidOperation()
    {
        var s = CriarValida();
        Assert.Throws<InvalidOperationException>(() => s.MarcarComoCriada());
    }

    [Test]
    public void MarcarComoCriada_IdValido_PublicaEvento()
    {
        var s = CriarValida();
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(s, 7L);

        s.MarcarComoCriada();

        Assert.That(s.DomainEvents, Has.Count.EqualTo(1));
        Assert.That(s.DomainEvents.First(), Is.TypeOf<SolicitacaoVinculoCriadaEvent>());
    }

    // ----- Aprovar -----

    [Test]
    public void Aprovar_Pendente_TransicionaParaAprovadaEPublicaEvento()
    {
        var s = CriarValida();
        var dono = Guid.NewGuid();
        s.Aprovar(dono);

        Assert.That(s.Status, Is.EqualTo(StatusSolicitacaoVinculo.Aprovada));
        Assert.That(s.RespondidaPorUsuarioId, Is.EqualTo(dono));
        Assert.That(s.RespondidaEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
        Assert.That(s.DomainEvents.OfType<SolicitacaoVinculoAprovadaEvent>(), Has.Exactly(1).Items);
    }

    [Test]
    public void Aprovar_NaoPendente_LancaBusinessException()
    {
        var s = CriarValida();
        s.Aprovar(Guid.NewGuid());
        var ex = Assert.Throws<BusinessException>(() => s.Aprovar(Guid.NewGuid()));
        Assert.That(ex.Message, Does.Contain("pendentes"));
    }

    [Test]
    public void Aprovar_RespondidoPorEmpty_LancaBusinessException()
    {
        var s = CriarValida();
        Assert.Throws<BusinessException>(() => s.Aprovar(Guid.Empty));
    }

    // ----- Recusar -----

    [Test]
    public void Recusar_Pendente_TransicionaERegistraMotivo()
    {
        var s = CriarValida();
        var dono = Guid.NewGuid();
        s.Recusar(dono, "Sem vagas");

        Assert.That(s.Status, Is.EqualTo(StatusSolicitacaoVinculo.Recusada));
        Assert.That(s.MotivoRecusa, Is.EqualTo("Sem vagas"));
        Assert.That(s.RespondidaPorUsuarioId, Is.EqualTo(dono));
        Assert.That(s.DomainEvents.OfType<SolicitacaoVinculoRecusadaEvent>(), Has.Exactly(1).Items);
    }

    [Test]
    public void Recusar_MotivoMuitoLongo_LancaBusinessException()
    {
        var s = CriarValida();
        var motivo = new string('a', 501);
        var ex = Assert.Throws<BusinessException>(() => s.Recusar(Guid.NewGuid(), motivo));
        Assert.That(ex.Message, Does.Contain("500"));
    }

    [Test]
    public void Recusar_NaoPendente_LancaBusinessException()
    {
        var s = CriarValida();
        s.Aprovar(Guid.NewGuid());
        Assert.Throws<BusinessException>(() => s.Recusar(Guid.NewGuid(), "tarde demais"));
    }

    [Test]
    public void Recusar_RespondidoPorEmpty_LancaBusinessException()
    {
        var s = CriarValida();
        Assert.Throws<BusinessException>(() => s.Recusar(Guid.Empty, "motivo"));
    }

    // ----- Cancelar -----

    [Test]
    public void Cancelar_Pendente_Transiciona()
    {
        var s = CriarValida();
        s.Cancelar();

        Assert.That(s.Status, Is.EqualTo(StatusSolicitacaoVinculo.Cancelada));
        Assert.That(s.RespondidaPorUsuarioId, Is.Null,
            "Cancelamento eh do proprio profissional — RespondidaPor fica null intencionalmente.");
    }

    [Test]
    public void Cancelar_NaoPendente_LancaBusinessException()
    {
        var s = CriarValida();
        s.Aprovar(Guid.NewGuid());
        Assert.Throws<BusinessException>(() => s.Cancelar());
    }
}
