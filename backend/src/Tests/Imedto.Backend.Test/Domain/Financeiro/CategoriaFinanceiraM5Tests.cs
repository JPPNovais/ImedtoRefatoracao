using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Financeiro;

/// <summary>
/// Testes de domínio para CategoriaFinanceira focados no M5 (briefing 2026-06-22_003).
/// R8: Padrao=true pode ser inativada (antes bloqueado); Atualizar() e exclusão permanecem bloqueados.
/// CA17, CA18, CA19.
/// </summary>
[TestFixture]
public class CategoriaFinanceiraM5Tests
{
    private static CategoriaFinanceira Padrao()
        => CategoriaFinanceira.CriarPadrao(1L, "Consultas", TipoCategoria.Receita);

    private static CategoriaFinanceira Customizada()
        => CategoriaFinanceira.Criar(1L, "Custom", TipoCategoria.Receita);

    // --- CA17: inativar padrão permitido ---

    [Test]
    public void Inativar_Padrao_PermitidoAposM5_CA17()
    {
        var c = Padrao();
        Assert.That(c.Ativo, Is.True);
        Assert.That(c.Padrao, Is.True);

        Assert.DoesNotThrow(() => c.Inativar());

        Assert.That(c.Ativo, Is.False);
        Assert.That(c.Padrao, Is.True, "Padrao deve permanecer true.");
    }

    [Test]
    public void Inativar_Customizada_Permitido()
    {
        var c = Customizada();
        Assert.DoesNotThrow(() => c.Inativar());
        Assert.That(c.Ativo, Is.False);
    }

    // --- CA18: renomear padrão segue bloqueado ---

    [Test]
    public void Atualizar_Padrao_SegueBloqueado_CA18()
    {
        var c = Padrao();
        var ex = Assert.Throws<BusinessException>(() => c.Atualizar("Novo Nome", TipoCategoria.Receita));
        Assert.That(ex!.Message, Does.Contain("padrão"));
        Assert.That(c.Nome, Is.EqualTo("Consultas"), "Nome não deve ter sido alterado.");
    }

    [Test]
    public void Atualizar_Customizada_Permitido()
    {
        var c = Customizada();
        Assert.DoesNotThrow(() => c.Atualizar("Novo", TipoCategoria.Despesa));
        Assert.That(c.Nome, Is.EqualTo("Novo"));
    }

    // --- CA19: reativar padrão inativada ---

    [Test]
    public void Reativar_PadraoInativada_Permitido_CA19()
    {
        var c = Padrao();
        c.Inativar(); // M5 permite
        Assert.That(c.Ativo, Is.False);

        c.Reativar();

        Assert.That(c.Ativo, Is.True);
        Assert.That(c.Padrao, Is.True);
    }

    // --- Inativar é idempotente ---

    [Test]
    public void Inativar_JaInativa_SemExcecao()
    {
        var c = Padrao();
        c.Inativar();
        Assert.DoesNotThrow(() => c.Inativar()); // segunda chamada silenciosa
        Assert.That(c.Ativo, Is.False);
    }

    // --- Reativar é idempotente ---

    [Test]
    public void Reativar_JaAtiva_SemExcecao()
    {
        var c = Padrao();
        Assert.DoesNotThrow(() => c.Reativar()); // já ativa
        Assert.That(c.Ativo, Is.True);
    }
}
