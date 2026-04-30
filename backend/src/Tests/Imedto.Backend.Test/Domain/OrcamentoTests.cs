using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;
using ItemPayload = Imedto.Backend.Domain.Orcamentos.Orcamento.ItemPayload;

namespace Imedto.Backend.Test.Domain;

[TestFixture]
public class OrcamentoTests
{
    private static IEnumerable<ItemPayload> UmItem() =>
        [new ItemPayload("Consulta", 1, 200m, 0)];

    private static DateOnly Amanha() => DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    private static Orcamento NovoOrcamento() =>
        Orcamento.Criar(1, 1, Amanha(), null, Guid.NewGuid(), null, itens: UmItem());

    [Test]
    public void Criar_ValidoComUmItem_CriaComStatusRascunho()
    {
        var orc = NovoOrcamento();
        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Rascunho));
        Assert.That(orc.Total, Is.EqualTo(200m));
    }

    [Test]
    public void Criar_SemItens_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Orcamento.Criar(1, 1, Amanha(), null, Guid.NewGuid(), null, itens: []));
        Assert.That(ex!.Message, Does.Contain("ao menos um item"));
    }

    [Test]
    public void Criar_ValidadePassada_LancaBusinessException()
    {
        var ontem = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var ex = Assert.Throws<BusinessException>(() =>
            Orcamento.Criar(1, 1, ontem, null, Guid.NewGuid(), null, itens: UmItem()));
        Assert.That(ex!.Message, Does.Contain("data passada"));
    }

    [Test]
    public void Criar_EstabelecimentoZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Orcamento.Criar(0, 1, Amanha(), null, Guid.NewGuid(), null, itens: UmItem()));
        Assert.That(ex!.Message, Does.Contain("Estabelecimento"));
    }

    [Test]
    public void Criar_PacienteZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Orcamento.Criar(1, 0, Amanha(), null, Guid.NewGuid(), null, itens: UmItem()));
        Assert.That(ex!.Message, Does.Contain("Paciente"));
    }

    [Test]
    public void Enviar_StatusRascunho_MudaParaEnviado()
    {
        var orc = NovoOrcamento();
        orc.Enviar();
        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Enviado));
    }

    [Test]
    public void Aprovar_StatusEnviado_MudaParaAprovado()
    {
        var orc = NovoOrcamento();
        orc.Enviar();
        orc.Aprovar();
        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Aprovado));
    }

    [Test]
    public void Aprovar_AindaRascunho_LancaBusinessException()
    {
        var orc = NovoOrcamento();
        var ex = Assert.Throws<BusinessException>(() => orc.Aprovar());
        Assert.That(ex!.Message, Does.Contain("enviados podem ser aprovados"));
    }

    [Test]
    public void Recusar_StatusEnviado_MudaParaRecusado()
    {
        var orc = NovoOrcamento();
        orc.Enviar();
        orc.Recusar();
        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Recusado));
    }

    [Test]
    public void Recusar_JaAprovado_LancaBusinessException()
    {
        var orc = NovoOrcamento();
        orc.Enviar();
        orc.Aprovar();
        var ex = Assert.Throws<BusinessException>(() => orc.Recusar());
        Assert.That(ex!.Message, Does.Contain("enviados podem ser recusados"));
    }

    [Test]
    public void Cancelar_StatusRascunho_MudaParaCancelado()
    {
        var orc = NovoOrcamento();
        orc.Cancelar();
        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Cancelado));
    }

    [Test]
    public void Cancelar_StatusAprovado_MudaParaCancelado()
    {
        var orc = NovoOrcamento();
        orc.Enviar();
        orc.Aprovar();
        orc.Cancelar();
        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Cancelado));
    }

    [Test]
    public void Cancelar_JaRecusado_LancaBusinessException()
    {
        var orc = NovoOrcamento();
        orc.Enviar();
        orc.Recusar();
        var ex = Assert.Throws<BusinessException>(() => orc.Cancelar());
        Assert.That(ex!.Message, Does.Contain("terminal"));
    }

    [Test]
    public void Expirar_StatusRascunho_MudaParaExpirado()
    {
        var orc = NovoOrcamento();
        orc.Expirar();
        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Expirado));
    }

    [Test]
    public void Expirar_JaAprovado_NaoMuda()
    {
        var orc = NovoOrcamento();
        orc.Enviar();
        orc.Aprovar();
        orc.Expirar();
        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Aprovado));
    }

    [Test]
    public void Total_ComDescontoParcial_CalculaCorretamente()
    {
        var itens = new[]
        {
            new ItemPayload("A", 2, 100m, 10),  // 2 * 100 * 0.9 = 180
            new ItemPayload("B", 1, 50m, 0),    // 50
        };
        var orc = Orcamento.Criar(1, 1, Amanha(), null, Guid.NewGuid(), null, itens: itens);
        Assert.That(orc.Total, Is.EqualTo(230m));
    }
}
