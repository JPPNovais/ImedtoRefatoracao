using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain;

[TestFixture]
public class LancamentoTests
{
    private static Lancamento CriarReceita() =>
        Lancamento.Criar(1, TipoLancamento.Receita, "Consulta", 300m,
            DateOnly.FromDateTime(DateTime.Today.AddDays(5)), "Consultas",
            Guid.NewGuid());

    [Test]
    public void Criar_Valido_StatusPendente()
    {
        var l = CriarReceita();
        Assert.That(l.Status, Is.EqualTo(StatusLancamento.Pendente));
    }

    [Test]
    public void Criar_ValorZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Lancamento.Criar(1, TipoLancamento.Receita, "x", 0m,
                DateOnly.FromDateTime(DateTime.Today), "Cat", Guid.NewGuid()));
        Assert.That(ex.Message, Does.Contain("maior que zero"));
    }

    [Test]
    public void Criar_DescricaoVazia_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Lancamento.Criar(1, TipoLancamento.Receita, "", 100m,
                DateOnly.FromDateTime(DateTime.Today), "Cat", Guid.NewGuid()));
        Assert.That(ex.Message, Does.Contain("obrigatória"));
    }

    [Test]
    public void Pagar_StatusPendente_MudaParaPago()
    {
        var l = CriarReceita();
        l.Pagar(DateOnly.FromDateTime(DateTime.Today));
        Assert.That(l.Status, Is.EqualTo(StatusLancamento.Pago));
    }

    [Test]
    public void Pagar_JaPago_LancaBusinessException()
    {
        var l = CriarReceita();
        l.Pagar(null);
        var ex = Assert.Throws<BusinessException>(() => l.Pagar(null));
        Assert.That(ex.Message, Does.Contain("pendentes podem ser baixados"));
    }

    [Test]
    public void Cancelar_StatusPendente_MudaParaCancelado()
    {
        var l = CriarReceita();
        l.Cancelar();
        Assert.That(l.Status, Is.EqualTo(StatusLancamento.Cancelado));
    }

    [Test]
    public void Cancelar_JaPago_LancaBusinessException()
    {
        var l = CriarReceita();
        l.Pagar(null);
        var ex = Assert.Throws<BusinessException>(() => l.Cancelar());
        Assert.That(ex.Message, Does.Contain("pago não pode ser cancelado"));
    }

    [Test]
    public void Cancelar_JaCancelado_LancaBusinessException()
    {
        var l = CriarReceita();
        l.Cancelar();
        var ex = Assert.Throws<BusinessException>(() => l.Cancelar());
        Assert.That(ex.Message, Does.Contain("cancelado"));
    }

    [Test]
    public void Atualizar_StatusCancelado_LancaBusinessException()
    {
        var l = CriarReceita();
        l.Cancelar();
        var ex = Assert.Throws<BusinessException>(() =>
            l.Atualizar("Nova desc", 100m,
                DateOnly.FromDateTime(DateTime.Today.AddDays(1)), "Cat"));
        Assert.That(ex.Message, Does.Contain("cancelado não pode ser editado"));
    }

    [Test]
    public void Atualizar_StatusPago_LancaBusinessException()
    {
        var l = CriarReceita();
        l.Pagar(null);
        var ex = Assert.Throws<BusinessException>(() =>
            l.Atualizar("Nova desc", 100m,
                DateOnly.FromDateTime(DateTime.Today.AddDays(1)), "Cat"));
        Assert.That(ex.Message, Does.Contain("pago não pode ser editado"));
    }
}
