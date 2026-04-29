using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;
using static Imedto.Backend.Domain.Orcamentos.Orcamento;

namespace Imedto.Backend.Test.Domain.Orcamentos;

[TestFixture]
public class OrcamentoCompletoTests
{
    private static DateOnly Amanha() => DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    /// <summary>Fábrica única para orçamento completo — use parâmetros nomeados para clareza.</summary>
    private static Orcamento CriarCompletoBase(
        IEnumerable<ItemPayload>? itens = null,
        IEnumerable<EquipePayload>? equipe = null,
        IEnumerable<ImplantePayload>? implantes = null,
        IEnumerable<FormaPagamentoPayload>? formas = null,
        IEnumerable<CirurgiaPayload>? cirurgias = null,
        InternacaoPayload? internacao = null,
        AnestesiaPayload? anestesia = null,
        decimal desconto = 0,
        decimal juros = 0)
    {
        itens ??= [new ItemPayload("Consulta cirúrgica", 1, 1000m, 0)];
        formas ??= [];

        return Orcamento.CriarCompleto(
            estabelecimentoId: 1,
            pacienteId: 1,
            validade: Amanha(),
            observacoes: null,
            criadoPorUsuarioId: Guid.NewGuid(),
            tipo: TipoOrcamento.Cirurgico,
            procedimentoCirurgicoId: null,
            configuracao: null,
            descontoBruto: desconto,
            jurosBrutos: juros,
            itens: itens,
            equipe: equipe ?? [],
            implantes: implantes ?? [],
            formasPagamento: formas,
            cirurgias: cirurgias,
            internacao: internacao,
            anestesia: anestesia);
    }

    [Test]
    public void ValidarIntegridade_SomaFormasConferemComTotal_NaoLancaExcecao()
    {
        var itens = new[] { new ItemPayload("Honorários", 1, 2000m, 0) };
        var formas = new[] { new FormaPagamentoPayload(1, 2000m, 1, 0m, 0m, null) };

        Assert.DoesNotThrow(() => CriarCompletoBase(itens: itens, formas: formas));
    }

    [Test]
    public void ValidarIntegridade_SomaFormasDiverge_LancaBusinessException()
    {
        var itens = new[] { new ItemPayload("Honorários", 1, 2000m, 0) };
        var formas = new[] { new FormaPagamentoPayload(1, 1500m, 1, 0m, 0m, null) }; // 500 a menos

        var ex = Assert.Throws<BusinessException>(() =>
            CriarCompletoBase(itens: itens, formas: formas));

        Assert.That(ex.Message, Does.Contain("não confere"));
    }

    [Test]
    public void ValidarIntegridade_ComDesconto_SomaFormasDeveBaterComTotalEfetivo()
    {
        var itens = new[] { new ItemPayload("Honorários", 1, 2000m, 0) };
        var formas = new[] { new FormaPagamentoPayload(1, 1800m, 1, 0m, 0m, null) }; // 2000 - 200 desconto
        var desconto = 200m;

        Assert.DoesNotThrow(() => CriarCompletoBase(itens: itens, formas: formas, desconto: desconto));
    }

    [Test]
    public void ValidarIntegridade_SemFormasPagamento_NaoValidaECriaOrcamento()
    {
        // Sem formas é válido (cotação em andamento)
        var orc = CriarCompletoBase();

        Assert.That(orc.FormasPagamento, Is.Empty);
        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Pendente));
    }

    [Test]
    public void AdicionarMembroEquipe_DuplicataPermitida_DocumentaComportamentoAtual()
    {
        // OrcamentoEquipe.AdicionarMembroEquipe não valida unicidade (diferente de ProcedimentoCirurgico).
        // Este teste documenta o comportamento atual: duplicatas são aceitas no Orçamento.
        var profissionalId = Guid.NewGuid();
        var orc = CriarCompletoBase(equipe: [new EquipePayload(profissionalId, "Cirurgião", 1000m)]);

        orc.AdicionarMembroEquipe(profissionalId, "Cirurgião", 1000m);

        Assert.That(orc.Equipe.Count(m => m.ProfissionalUsuarioId == profissionalId), Is.EqualTo(2));
    }

    [Test]
    public void AdicionarFormaPagamento_OrcamentoPendente_AdicionaCorretamente()
    {
        var orc = CriarCompletoBase();

        orc.AdicionarFormaPagamento(1, 1000m, 1, 0m, 0m, null);

        Assert.That(orc.FormasPagamento, Has.Count.EqualTo(1));
        Assert.That(orc.FormasPagamento[0].Valor, Is.EqualTo(1000m));
    }

    [Test]
    public void AdicionarFormaPagamento_OrcamentoAprovado_LancaBusinessException()
    {
        var orc = Orcamento.Criar(1, 1, Amanha(), null, Guid.NewGuid(),
            [new ItemPayload("Item", 1, 100m, 0)]);
        orc.Aprovar();

        var ex = Assert.Throws<BusinessException>(() =>
            orc.AdicionarFormaPagamento(1, 100m, 1, 0m, 0m, null));

        Assert.That(ex.Message, Does.Contain("pendentes podem ser editados"));
    }

    [Test]
    public void AdicionarImplante_OrcamentoPendente_CustoTotalAtualizado()
    {
        var orc = CriarCompletoBase();

        orc.AdicionarImplante(null, "Prótese titanium", 1m, 5000m);

        Assert.That(orc.CustoImplantesTotal, Is.EqualTo(5000m));
    }

    [Test]
    public void RemoverImplante_ImplanteExistente_ImplanteRemovidoETotalAtualizado()
    {
        // Começa com orçamento limpo, adiciona dois implantes em memória (Id=0 para ambos).
        // Remove pelo Id=0 (pega o primeiro encontrado), verifica que sobrou 1 e o custo foi recalculado.
        var orc = CriarCompletoBase();
        orc.AdicionarImplante(null, "Prótese titanium", 1m, 5000m);
        orc.AdicionarImplante(null, "Parafuso", 2m, 100m);
        // Remove qualquer implante (Id=0 em memória — mesma referência do primeiro adicionado)
        var qualquer = orc.Implantes.First();

        orc.RemoverImplante(qualquer.Id);

        Assert.That(orc.Implantes, Has.Count.EqualTo(1));
        // Custo restante deve ser o do implante que sobrou
        Assert.That(orc.CustoImplantesTotal, Is.EqualTo(orc.Implantes[0].CustoTotal));
    }

    [Test]
    public void RemoverImplante_ImplanteNaoEncontrado_LancaBusinessException()
    {
        var orc = CriarCompletoBase();

        var ex = Assert.Throws<BusinessException>(() => orc.RemoverImplante(99));

        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }

    [Test]
    public void CriarCompleto_SemItensNemEquipeNemImplantes_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Orcamento.CriarCompleto(1, 1, Amanha(), null, Guid.NewGuid(),
                TipoOrcamento.Cirurgico, null, null, 0, 0,
                [], [], [], []));

        Assert.That(ex.Message, Does.Contain("ao menos um item"));
    }

    [Test]
    public void CalcularTotalEfetivo_ComDescontoEJuros_CalculaCorreto()
    {
        var orc = CriarCompletoBase(itens: [new ItemPayload("Item", 1, 1000m, 0)]);

        var efetivo = orc.CalcularTotalEfetivo(descontoBruto: 100m, jurosBrutos: 50m);

        Assert.That(efetivo, Is.EqualTo(950m)); // 1000 - 100 + 50
    }
}
