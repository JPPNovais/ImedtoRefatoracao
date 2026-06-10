using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Inventario.Events;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Inventario;

[TestFixture]
public class ItemInventarioTests
{
    private static ItemInventario CriarItem(decimal quantidadeMinima = 5m) =>
        ItemInventario.Criar(
            estabelecimentoId: 1,
            codigo: "MED001",
            nome: "Seringa",
            categoriaId: 10,
            categoriaNomeSnapshot: "Material",
            unidadeMedida: "un",
            quantidadeMinima: quantidadeMinima,
            fabricanteId: null,
            fornecedorPadraoId: null,
            localPadraoId: null,
            custoUnitario: null);

    // --- CustoMedio ---

    [Test]
    public void RegistrarEntrada_EstoqueVazio_CustoMedioIgualAoCustoUnitario()
    {
        var item = CriarItem();

        item.RegistrarEntrada(quantidade: 10, usuarioId: Guid.NewGuid(), custoUnitario: 5m, observacao: null);

        Assert.That(item.CustoMedio, Is.EqualTo(5m));
    }

    [Test]
    public void RegistrarEntrada_DuasEntradas_CustoMedioPonderadoCorreto()
    {
        // 10 un × R$5 + 10 un × R$7 → CustoMedio = R$6
        var item = CriarItem();
        item.RegistrarEntrada(10, Guid.NewGuid(), 5m, null);

        item.RegistrarEntrada(10, Guid.NewGuid(), 7m, null);

        Assert.That(item.CustoMedio, Is.EqualTo(6m));
    }

    [Test]
    public void RegistrarSaida_UsaCustoMedioComoSnapshot_CustoUnitarioDaMovimentacao()
    {
        // Após 10@5 + 10@7 (CustoMedio=6), saída de 5 un → CustoUnitario na movimentação = 6
        var item = CriarItem();
        item.RegistrarEntrada(10, Guid.NewGuid(), 5m, null);
        item.RegistrarEntrada(10, Guid.NewGuid(), 7m, null);

        var mov = item.RegistrarSaida(quantidade: 5, usuarioId: Guid.NewGuid(), observacao: null);

        Assert.That(mov.CustoUnitario, Is.EqualTo(6m));
        Assert.That(mov.CustoTotal, Is.EqualTo(30m));
    }

    [Test]
    public void RegistrarSaida_NaoAlteraCustoMedioDoItem()
    {
        var item = CriarItem();
        item.RegistrarEntrada(10, Guid.NewGuid(), 5m, null);
        item.RegistrarEntrada(10, Guid.NewGuid(), 7m, null);
        var custoMedioAntes = item.CustoMedio;

        item.RegistrarSaida(5, Guid.NewGuid(), null);

        Assert.That(item.CustoMedio, Is.EqualTo(custoMedioAntes));
    }

    [Test]
    public void RegistrarEntrada_CustoUnitarioNegativo_LancaBusinessException()
    {
        var item = CriarItem();

        var ex = Assert.Throws<BusinessException>(() =>
            item.RegistrarEntrada(10, Guid.NewGuid(), custoUnitario: -1m, null));

        Assert.That(ex.Message, Does.Contain("negativo").IgnoreCase);
    }

    [Test]
    public void RegistrarEntrada_CustoUnitarioZero_NaoLancaExcecao()
    {
        // custoUnitario == 0 é permitido (item sem custo registrado — validação é >= 0)
        var item = CriarItem();

        Assert.DoesNotThrow(() =>
            item.RegistrarEntrada(10, Guid.NewGuid(), custoUnitario: 0m, null));
    }

    [Test]
    public void RegistrarSaida_QuantidadeAcimaDoEstoque_LancaBusinessException()
    {
        var item = CriarItem();
        item.RegistrarEntrada(5, Guid.NewGuid(), 10m, null);

        var ex = Assert.Throws<BusinessException>(() =>
            item.RegistrarSaida(quantidade: 6, Guid.NewGuid(), null));

        Assert.That(ex.Message, Does.Contain("insuficiente").IgnoreCase);
    }

    [Test]
    public void RegistrarEntrada_RetornaMovimentacaoComCamposCorretos()
    {
        var item = CriarItem();
        var usuario = Guid.NewGuid();

        var mov = item.RegistrarEntrada(8, usuario, 12m, "Nota fiscal 123");

        Assert.That(mov.Quantidade, Is.EqualTo(8m));
        Assert.That(mov.CustoUnitario, Is.EqualTo(12m));
        Assert.That(mov.CustoTotal, Is.EqualTo(96m));
        Assert.That(mov.CriadoPorUsuarioId, Is.EqualTo(usuario));
        Assert.That(mov.Tipo, Is.EqualTo(TipoMovimentacaoEstoque.Entrada));
    }

    // --- Alerta de estoque mínimo (CA1-CA5) ---

    /// <summary>CA1: saída que cruza o mínimo dispara o evento.</summary>
    [Test]
    public void RegistrarSaida_CruzaMinimoDescendente_DispararaEvento()
    {
        // 12 un, mínimo 10 → saída de 5 → saldo 7 (< 10): cruza.
        var item = CriarItem(quantidadeMinima: 10m);
        item.RegistrarEntrada(12m, Guid.NewGuid(), 2m, null);

        item.RegistrarSaida(5m, Guid.NewGuid(), null);

        var evento = item.DomainEvents.OfType<EstoqueAbaixoMinimoEvent>().SingleOrDefault();
        Assert.That(evento, Is.Not.Null, "Esperava EstoqueAbaixoMinimoEvent ao cruzar o mínimo.");
        Assert.That(evento!.QuantidadeAtual, Is.EqualTo(7m));
        Assert.That(evento.QuantidadeMinima, Is.EqualTo(10m));
    }

    /// <summary>CA2: item já abaixo do mínimo — nova saída NÃO dispara de novo (anti-spam).</summary>
    [Test]
    public void RegistrarSaida_JaAbaixoDoMinimo_NaoDisparaEvento()
    {
        // Item começa com 12, mínimo 10.
        // Primeira saída (5) → cruza (saldo 7). Segunda saída (2) → já estava abaixo, não dispara.
        var item = CriarItem(quantidadeMinima: 10m);
        item.RegistrarEntrada(12m, Guid.NewGuid(), 2m, null);
        item.RegistrarSaida(5m, Guid.NewGuid(), null); // cruzamento → gera evento
        item.ClearDomainEvents();                      // limpa — simula o dispatch do handler

        item.RegistrarSaida(2m, Guid.NewGuid(), null); // já abaixo → não deve disparar

        var evento = item.DomainEvents.OfType<EstoqueAbaixoMinimoEvent>().FirstOrDefault();
        Assert.That(evento, Is.Null, "Não deve disparar evento para item já abaixo do mínimo.");
    }

    /// <summary>CA3: reabastecer e cair de novo gera novo alerta.</summary>
    [Test]
    public void RegistrarSaida_ReCruzamentoAposReabastecimento_DispararaEvento()
    {
        // Começa com saldo 5 (abaixo de 10). Reabastecer: entrada de 20 → saldo 20 (>= 10).
        // Nova saída de 16 → saldo 4 (< 10): cruzamento descendente → deve alertar.
        var item = CriarItem(quantidadeMinima: 10m);
        item.RegistrarEntrada(5m, Guid.NewGuid(), 2m, null);  // saldo 5 (já abaixo — sem evento)
        item.RegistrarEntrada(20m, Guid.NewGuid(), 2m, null); // saldo 25 (>= mínimo após reabastecimento)
        item.ClearDomainEvents(); // garante slate limpo antes da saída crítica

        item.RegistrarSaida(16m, Guid.NewGuid(), null); // saldo 9 (< 10) → novo cruzamento

        var evento = item.DomainEvents.OfType<EstoqueAbaixoMinimoEvent>().SingleOrDefault();
        Assert.That(evento, Is.Not.Null, "Deve disparar evento para novo cruzamento após reabastecimento.");
    }

    /// <summary>CA4: entrada nunca dispara o evento de alerta.</summary>
    [Test]
    public void RegistrarEntrada_AbaixoDoMinimo_NaoDisparaEvento()
    {
        // Item em 5 (abaixo de 10). Entrada de 2 → saldo 7 (ainda abaixo). Não deve disparar.
        var item = CriarItem(quantidadeMinima: 10m);
        item.RegistrarEntrada(5m, Guid.NewGuid(), 2m, null); // saldo 5

        item.RegistrarEntrada(2m, Guid.NewGuid(), 2m, null); // saldo 7 — ainda abaixo, mas entrada não dispara

        var evento = item.DomainEvents.OfType<EstoqueAbaixoMinimoEvent>().FirstOrDefault();
        Assert.That(evento, Is.Null, "Entrada nunca deve disparar o evento de estoque mínimo.");
    }

    /// <summary>CA5: mínimo zero = sem alerta (qualquer saída fica sempre >= 0).</summary>
    [Test]
    public void RegistrarSaida_MinimoZero_NaoDisparaEvento()
    {
        var item = CriarItem(quantidadeMinima: 0m);
        item.RegistrarEntrada(10m, Guid.NewGuid(), 2m, null);

        item.RegistrarSaida(10m, Guid.NewGuid(), null); // saldo 0, mínimo 0 → não cruza (0 < 0 = false)

        var evento = item.DomainEvents.OfType<EstoqueAbaixoMinimoEvent>().FirstOrDefault();
        Assert.That(evento, Is.Null, "Mínimo zero não deve gerar alerta.");
    }
}
