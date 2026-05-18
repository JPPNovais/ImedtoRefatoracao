using Imedto.Backend.Domain.PedidosExame;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.PedidosExame;

[TestFixture]
public class PedidoExameTests
{
    [Test]
    public void Emitir_ComExamesEIndicacao_RetornaPedido()
    {
        var p = PedidoExame.Emitir(
            estabelecimentoId: 1, pacienteId: 10, profissionalUsuarioId: Guid.NewGuid(),
            tipo: TipoPedidoExame.Laboratorial,
            exames: new[] { "Hemograma completo", "Glicose em jejum" },
            indicacaoClinica: "Investigação de anemia",
            cid10: null, observacoes: null);

        Assert.That(p.Exames, Has.Count.EqualTo(2));
        Assert.That(p.IndicacaoClinica, Is.EqualTo("Investigação de anemia"));
        Assert.That(p.Tipo, Is.EqualTo(TipoPedidoExame.Laboratorial));
    }

    [Test]
    public void Emitir_ExamesVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() => PedidoExame.Emitir(
            1, 10, Guid.NewGuid(), TipoPedidoExame.Laboratorial,
            exames: Array.Empty<string>(), indicacaoClinica: "Investigação", cid10: null, observacoes: null));
        Assert.That(ex.Message, Does.Contain("exame"));
    }

    [Test]
    public void Emitir_ExamesNull_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => PedidoExame.Emitir(
            1, 10, Guid.NewGuid(), TipoPedidoExame.Laboratorial,
            exames: null, indicacaoClinica: "Investigação", cid10: null, observacoes: null));
    }

    [Test]
    public void Emitir_ExamesSoEspacos_TratadoComoVazio()
    {
        Assert.Throws<BusinessException>(() => PedidoExame.Emitir(
            1, 10, Guid.NewGuid(), TipoPedidoExame.Laboratorial,
            exames: new[] { "   ", "" }, indicacaoClinica: "Investigação", cid10: null, observacoes: null));
    }

    [Test]
    public void Emitir_IndicacaoVazia_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() => PedidoExame.Emitir(
            1, 10, Guid.NewGuid(), TipoPedidoExame.Laboratorial,
            exames: new[] { "Hemograma" }, indicacaoClinica: "  ", cid10: null, observacoes: null));
        Assert.That(ex.Message, Does.Contain("Indicação"));
    }

    [Test]
    public void Emitir_Cid10Invalido_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => PedidoExame.Emitir(
            1, 10, Guid.NewGuid(), TipoPedidoExame.Imagem,
            exames: new[] { "RX tórax" }, indicacaoClinica: "Tosse",
            cid10: "999", observacoes: null));
    }

    [Test]
    public void Emitir_Cid10Valido_Normaliza()
    {
        var p = PedidoExame.Emitir(
            1, 10, Guid.NewGuid(), TipoPedidoExame.Imagem,
            exames: new[] { "RX tórax" }, indicacaoClinica: "Tosse",
            cid10: "j06.9", observacoes: null);
        Assert.That(p.Cid10, Is.EqualTo("J06.9"));
    }

    [Test]
    public void Emitir_ListaCom51Exames_LancaBusinessException()
    {
        var muitos = Enumerable.Range(0, 51).Select(i => $"Exame {i}").ToArray();
        Assert.Throws<BusinessException>(() => PedidoExame.Emitir(
            1, 10, Guid.NewGuid(), TipoPedidoExame.Laboratorial,
            exames: muitos, indicacaoClinica: "Indicação", cid10: null, observacoes: null));
    }
}
