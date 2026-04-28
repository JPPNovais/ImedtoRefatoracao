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

    [Test]
    public void Criar_ValidoComUmItem_CriaComStatusPendente()
    {
        var orc = Orcamento.Criar(1, 1, Amanha(), null, Guid.NewGuid(), UmItem());
        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Pendente));
        Assert.That(orc.Total, Is.EqualTo(200m));
    }

    [Test]
    public void Criar_SemItens_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Orcamento.Criar(1, 1, Amanha(), null, Guid.NewGuid(), []));
        Assert.That(ex.Message, Does.Contain("ao menos um item"));
    }

    [Test]
    public void Criar_ValidadePassada_LancaBusinessException()
    {
        var ontem = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var ex = Assert.Throws<BusinessException>(() =>
            Orcamento.Criar(1, 1, ontem, null, Guid.NewGuid(), UmItem()));
        Assert.That(ex.Message, Does.Contain("data passada"));
    }

    [Test]
    public void Criar_EstabelecimentoZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Orcamento.Criar(0, 1, Amanha(), null, Guid.NewGuid(), UmItem()));
        Assert.That(ex.Message, Does.Contain("Estabelecimento"));
    }

    [Test]
    public void Criar_PacienteZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Orcamento.Criar(1, 0, Amanha(), null, Guid.NewGuid(), UmItem()));
        Assert.That(ex.Message, Does.Contain("Paciente"));
    }

    [Test]
    public void Aprovar_StatusPendente_MudaParaAprovado()
    {
        var orc = Orcamento.Criar(1, 1, Amanha(), null, Guid.NewGuid(), UmItem());
        orc.Aprovar();
        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Aprovado));
    }

    [Test]
    public void Aprovar_JaAprovado_LancaBusinessException()
    {
        var orc = Orcamento.Criar(1, 1, Amanha(), null, Guid.NewGuid(), UmItem());
        orc.Aprovar();
        var ex = Assert.Throws<BusinessException>(() => orc.Aprovar());
        Assert.That(ex.Message, Does.Contain("pendentes podem ser aprovados"));
    }

    [Test]
    public void Recusar_StatusPendente_MudaParaRecusado()
    {
        var orc = Orcamento.Criar(1, 1, Amanha(), null, Guid.NewGuid(), UmItem());
        orc.Recusar();
        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Recusado));
    }

    [Test]
    public void Recusar_JaAprovado_LancaBusinessException()
    {
        var orc = Orcamento.Criar(1, 1, Amanha(), null, Guid.NewGuid(), UmItem());
        orc.Aprovar();
        var ex = Assert.Throws<BusinessException>(() => orc.Recusar());
        Assert.That(ex.Message, Does.Contain("pendentes podem ser recusados"));
    }

    [Test]
    public void Expirar_StatusPendente_MudaParaExpirado()
    {
        var orc = Orcamento.Criar(1, 1, Amanha(), null, Guid.NewGuid(), UmItem());
        orc.Expirar();
        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Expirado));
    }

    [Test]
    public void Total_ComDescontoParcial_CalculaCorretamente()
    {
        var itens = new[]
        {
            new ItemPayload("A", 2, 100m, 10),  // 2 * 100 * 0.9 = 180
            new ItemPayload("B", 1, 50m, 0),    // 50
        };
        var orc = Orcamento.Criar(1, 1, Amanha(), null, Guid.NewGuid(), itens);
        Assert.That(orc.Total, Is.EqualTo(230m));
    }
}
