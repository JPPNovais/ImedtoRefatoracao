using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain;

/// <summary>
/// Testes de domínio para o aggregate CaixaDiario (CA163–CA169).
/// Cobre: Abrir, Fechar, Reabrir + invariantes de estado.
/// </summary>
[TestFixture]
public class CaixaDiarioTests
{
    private const long EstabelecimentoId = 42;
    private readonly Guid _usuario1 = Guid.NewGuid();
    private readonly Guid _usuario2 = Guid.NewGuid();
    private readonly DateOnly _hoje = DateOnly.FromDateTime(DateTime.Today);

    // ─── Abrir ────────────────────────────────────────────────────────────────

    [Test]
    public void Abrir_DadosValidos_CriaStatusAberto()
    {
        var caixa = CaixaDiario.Abrir(EstabelecimentoId, _hoje, _usuario1);

        Assert.That(caixa.EstabelecimentoId, Is.EqualTo(EstabelecimentoId));
        Assert.That(caixa.Data, Is.EqualTo(_hoje));
        Assert.That(caixa.Status, Is.EqualTo(StatusCaixaDiario.Aberto));
        Assert.That(caixa.AbertoPorUsuarioId, Is.EqualTo(_usuario1));
        Assert.That(caixa.AbertoEm, Is.Not.EqualTo(default(DateTime)));
        Assert.That(caixa.FechadoEm, Is.Null);
        Assert.That(caixa.ReabertoEm, Is.Null);
    }

    // ─── Fechar ───────────────────────────────────────────────────────────────

    [Test]
    public void Fechar_CaixaAberto_AtualizaStatusEFechadoPor()
    {
        var caixa = CaixaDiario.Abrir(EstabelecimentoId, _hoje, _usuario1);
        caixa.Fechar(_usuario2, "Fechamento normal");

        Assert.That(caixa.Status, Is.EqualTo(StatusCaixaDiario.Fechado));
        Assert.That(caixa.FechadoPorUsuarioId, Is.EqualTo(_usuario2));
        Assert.That(caixa.FechadoEm, Is.Not.Null);
        Assert.That(caixa.Observacao, Is.EqualTo("Fechamento normal"));
    }

    [Test]
    public void Fechar_CaixaJaFechado_LancaBusinessException()
    {
        var caixa = CaixaDiario.Abrir(EstabelecimentoId, _hoje, _usuario1);
        caixa.Fechar(_usuario2, null);

        Assert.Throws<BusinessException>(() => caixa.Fechar(_usuario2, null));
    }

    [Test]
    public void Fechar_SemObservacao_Permitido()
    {
        var caixa = CaixaDiario.Abrir(EstabelecimentoId, _hoje, _usuario1);
        caixa.Fechar(_usuario2, null);

        Assert.That(caixa.Status, Is.EqualTo(StatusCaixaDiario.Fechado));
        Assert.That(caixa.Observacao, Is.Null);
    }

    // ─── Reabrir ──────────────────────────────────────────────────────────────

    [Test]
    public void Reabrir_CaixaFechado_AtualizaStatusParaAberto()
    {
        var caixa = CaixaDiario.Abrir(EstabelecimentoId, _hoje, _usuario1);
        caixa.Fechar(_usuario2, null);
        caixa.Reabrir(_usuario1);

        Assert.That(caixa.Status, Is.EqualTo(StatusCaixaDiario.Aberto));
        Assert.That(caixa.ReabertoPorUsuarioId, Is.EqualTo(_usuario1));
        Assert.That(caixa.ReabertoEm, Is.Not.Null);
    }

    [Test]
    public void Reabrir_CaixaJaAberto_LancaBusinessException()
    {
        var caixa = CaixaDiario.Abrir(EstabelecimentoId, _hoje, _usuario1);

        Assert.Throws<BusinessException>(() => caixa.Reabrir(_usuario1));
    }

    // ─── Ciclo completo ───────────────────────────────────────────────────────

    [Test]
    public void Ciclo_AbrirFecharReabrir_EstadosCorretos()
    {
        var caixa = CaixaDiario.Abrir(EstabelecimentoId, _hoje, _usuario1);
        Assert.That(caixa.Status, Is.EqualTo(StatusCaixaDiario.Aberto));

        caixa.Fechar(_usuario2, "Primeiro fechamento");
        Assert.That(caixa.Status, Is.EqualTo(StatusCaixaDiario.Fechado));

        caixa.Reabrir(_usuario1);
        Assert.That(caixa.Status, Is.EqualTo(StatusCaixaDiario.Aberto));

        caixa.Fechar(_usuario2, "Segundo fechamento");
        Assert.That(caixa.Status, Is.EqualTo(StatusCaixaDiario.Fechado));
    }
}
