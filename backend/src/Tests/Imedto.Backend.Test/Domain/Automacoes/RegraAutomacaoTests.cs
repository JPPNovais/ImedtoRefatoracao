using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Automacoes;

[TestFixture]
public class RegraAutomacaoTests
{
    private static RegraAutomacao CriarValida() =>
        RegraAutomacao.Criar(
            estabelecimentoId: 1L,
            nome: " Lembrete 24h ",
            eventoGatilho: " agendamento-criado ",
            condicoesJson: "[]",
            acoesJson: "[{\"tipo\":\"whatsapp\"}]");

    // ----- Criar -----

    [Test]
    public void Criar_Valida_StateOk()
    {
        var r = CriarValida();
        Assert.That(r.Nome, Is.EqualTo("Lembrete 24h"));
        Assert.That(r.EventoGatilho, Is.EqualTo("agendamento-criado"));
        Assert.That(r.CondicoesJson, Is.EqualTo("[]"));
        Assert.That(r.AcoesJson, Is.EqualTo("[{\"tipo\":\"whatsapp\"}]"));
        Assert.That(r.Ativa, Is.True);
        Assert.That(r.CriadoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void Criar_CondicoesVazias_AdotaArrayVazio()
    {
        var r = RegraAutomacao.Criar(1L, "X", "evt", " ", "[{}]");
        Assert.That(r.CondicoesJson, Is.EqualTo("[]"));
    }

    [Test]
    public void Criar_EstabelecimentoZero_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            RegraAutomacao.Criar(0L, "X", "evt", "[]", "[{}]"));
    }

    [Test]
    public void Criar_NomeVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            RegraAutomacao.Criar(1L, " ", "evt", "[]", "[{}]"));
    }

    [Test]
    public void Criar_NomeMaiorQue120_LancaBusinessException()
    {
        var nome = new string('a', 121);
        var ex = Assert.Throws<BusinessException>(() =>
            RegraAutomacao.Criar(1L, nome, "evt", "[]", "[{}]"));
        Assert.That(ex.Message, Does.Contain("120"));
    }

    [Test]
    public void Criar_EventoVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            RegraAutomacao.Criar(1L, "X", " ", "[]", "[{}]"));
    }

    [Test]
    public void Criar_EventoMaiorQue60_LancaBusinessException()
    {
        var evt = new string('e', 61);
        var ex = Assert.Throws<BusinessException>(() =>
            RegraAutomacao.Criar(1L, "X", evt, "[]", "[{}]"));
        Assert.That(ex.Message, Does.Contain("60"));
    }

    [Test]
    public void Criar_AcoesVazias_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            RegraAutomacao.Criar(1L, "X", "evt", "[]", " "));
        Assert.That(ex.Message, Does.Contain("ação"));
    }

    // ----- AtualizarRegras -----

    [Test]
    public void AtualizarRegras_Valido_AtualizaCampos()
    {
        var r = CriarValida();
        r.AtualizarRegras("Novo", "agendamento-cancelado", "[{\"campo\":\"prioridade\"}]", "[{\"tipo\":\"email\"}]");

        Assert.That(r.Nome, Is.EqualTo("Novo"));
        Assert.That(r.EventoGatilho, Is.EqualTo("agendamento-cancelado"));
        Assert.That(r.CondicoesJson, Is.EqualTo("[{\"campo\":\"prioridade\"}]"));
        Assert.That(r.AcoesJson, Is.EqualTo("[{\"tipo\":\"email\"}]"));
        Assert.That(r.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void AtualizarRegras_NomeVazio_LancaBusinessException()
    {
        var r = CriarValida();
        Assert.Throws<BusinessException>(() =>
            r.AtualizarRegras(" ", "evt", "[]", "[{}]"));
    }

    // ----- Ativar / Desativar -----

    [Test]
    public void Desativar_Ativa_FicaInativaEAtualiza()
    {
        var r = CriarValida();
        r.Desativar();
        Assert.That(r.Ativa, Is.False);
        Assert.That(r.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void Desativar_JaInativa_NaoMarcaAtualizada()
    {
        var r = CriarValida();
        r.Desativar();
        var antes = r.AtualizadoEm;
        r.Desativar();
        Assert.That(r.AtualizadoEm, Is.EqualTo(antes));
    }

    [Test]
    public void Ativar_Inativa_FicaAtivaEAtualiza()
    {
        var r = CriarValida();
        r.Desativar();
        var antes = r.AtualizadoEm;
        // Garante diferença temporal observável.
        Thread.Sleep(2);
        r.Ativar();
        Assert.That(r.Ativa, Is.True);
        Assert.That(r.AtualizadoEm, Is.GreaterThan(antes));
    }

    [Test]
    public void Ativar_JaAtiva_NaoMarcaAtualizada()
    {
        var r = CriarValida();
        var antes = r.AtualizadoEm;
        r.Ativar();
        Assert.That(r.AtualizadoEm, Is.EqualTo(antes));
    }
}
