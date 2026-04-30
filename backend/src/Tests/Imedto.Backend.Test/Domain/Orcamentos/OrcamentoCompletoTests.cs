using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;
using static Imedto.Backend.Domain.Orcamentos.Orcamento;

namespace Imedto.Backend.Test.Domain.Orcamentos;

[TestFixture]
public class OrcamentoCompletoTests
{
    private static DateOnly Amanha() => DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    /// <summary>Helper para orçamentos com múltiplas collections via fábrica única.</summary>
    private static Orcamento CriarOrcamento(
        IEnumerable<ItemPayload>? itens = null,
        IEnumerable<EquipePayload>? equipe = null,
        IEnumerable<ImplantePayload>? implantes = null,
        IEnumerable<FormaPagamentoPayload>? formas = null,
        IEnumerable<CirurgiaPayload>? cirurgias = null,
        InternacaoPayload? internacao = null,
        AnestesiaPayload? anestesia = null)
    {
        itens ??= [new ItemPayload("Consulta cirúrgica", 1, 1000m, 0)];

        return Orcamento.Criar(
            estabelecimentoId: 1,
            pacienteId: 1,
            validade: Amanha(),
            observacoes: null,
            criadoPorUsuarioId: Guid.NewGuid(),
            procedimentoCirurgicoId: null,
            itens: itens,
            equipe: equipe,
            implantes: implantes,
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

        Assert.DoesNotThrow(() => CriarOrcamento(itens: itens, formas: formas));
    }

    [Test]
    public void ValidarIntegridade_SomaFormasDiverge_LancaBusinessException()
    {
        var itens = new[] { new ItemPayload("Honorários", 1, 2000m, 0) };
        var formas = new[] { new FormaPagamentoPayload(1, 1500m, 1, 0m, 0m, null) }; // 500 a menos

        var ex = Assert.Throws<BusinessException>(() =>
            CriarOrcamento(itens: itens, formas: formas));

        Assert.That(ex!.Message, Does.Contain("não confere"));
    }

    [Test]
    public void ValidarIntegridade_SemFormasPagamento_NaoValidaECriaOrcamento()
    {
        var orc = CriarOrcamento();

        Assert.That(orc.FormasPagamento, Is.Empty);
        Assert.That(orc.Status, Is.EqualTo(OrcamentoStatus.Rascunho));
    }

    [Test]
    public void AdicionarMembroEquipe_DuplicataPermitida_DocumentaComportamentoAtual()
    {
        var profissionalId = Guid.NewGuid();
        var orc = CriarOrcamento(equipe: [new EquipePayload(profissionalId, "Cirurgião", 1000m)]);

        orc.AdicionarMembroEquipe(profissionalId, "Cirurgião", 1000m);

        Assert.That(orc.Equipe.Count(m => m.ProfissionalUsuarioId == profissionalId), Is.EqualTo(2));
    }

    [Test]
    public void AdicionarFormaPagamento_OrcamentoRascunho_AdicionaCorretamente()
    {
        var orc = CriarOrcamento();

        orc.AdicionarFormaPagamento(1, 1000m, 1, 0m, 0m, null);

        Assert.That(orc.FormasPagamento, Has.Count.EqualTo(1));
        Assert.That(orc.FormasPagamento[0].Valor, Is.EqualTo(1000m));
    }

    [Test]
    public void AdicionarFormaPagamento_OrcamentoAprovado_LancaBusinessException()
    {
        var orc = CriarOrcamento();
        orc.Enviar();
        orc.Aprovar();

        var ex = Assert.Throws<BusinessException>(() =>
            orc.AdicionarFormaPagamento(1, 100m, 1, 0m, 0m, null));

        Assert.That(ex!.Message, Does.Contain("rascunho ou enviados"));
    }

    [Test]
    public void AdicionarImplante_OrcamentoRascunho_CustoTotalAtualizado()
    {
        var orc = CriarOrcamento();

        orc.AdicionarImplante(null, "Prótese titanium", 1m, 5000m);

        Assert.That(orc.CustoImplantesTotal, Is.EqualTo(5000m));
    }

    [Test]
    public void RemoverImplante_ImplanteExistente_ImplanteRemovidoETotalAtualizado()
    {
        var orc = CriarOrcamento();
        orc.AdicionarImplante(null, "Prótese titanium", 1m, 5000m);
        orc.AdicionarImplante(null, "Parafuso", 2m, 100m);
        var qualquer = orc.Implantes.First();

        orc.RemoverImplante(qualquer.Id);

        Assert.That(orc.Implantes, Has.Count.EqualTo(1));
        Assert.That(orc.CustoImplantesTotal, Is.EqualTo(orc.Implantes[0].CustoTotal));
    }

    [Test]
    public void RemoverImplante_ImplanteNaoEncontrado_LancaBusinessException()
    {
        var orc = CriarOrcamento();

        var ex = Assert.Throws<BusinessException>(() => orc.RemoverImplante(99));

        Assert.That(ex!.Message, Does.Contain("não encontrado"));
    }

    [Test]
    public void Criar_SemItensNemEquipeNemImplantesNemCirurgias_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Orcamento.Criar(1, 1, Amanha(), null, Guid.NewGuid(), null,
                itens: [], equipe: [], implantes: [], formasPagamento: []));

        Assert.That(ex!.Message, Does.Contain("ao menos um item"));
    }

    [Test]
    public void RegistrarConversaoEmProcedimento_StatusAprovado_VinculaProcedimento()
    {
        var orc = CriarOrcamento();
        orc.Enviar();
        orc.Aprovar();

        orc.RegistrarConversaoEmProcedimento(42L);

        Assert.That(orc.ProcedimentoCirurgicoId, Is.EqualTo(42L));
    }

    [Test]
    public void RegistrarConversaoEmProcedimento_AindaRascunho_LancaBusinessException()
    {
        var orc = CriarOrcamento();
        var ex = Assert.Throws<BusinessException>(() => orc.RegistrarConversaoEmProcedimento(42L));
        Assert.That(ex!.Message, Does.Contain("aprovados"));
    }

    [Test]
    public void RegistrarConversaoEmProcedimento_JaConvertido_LancaBusinessException()
    {
        var orc = CriarOrcamento();
        orc.Enviar();
        orc.Aprovar();
        orc.RegistrarConversaoEmProcedimento(42L);

        var ex = Assert.Throws<BusinessException>(() => orc.RegistrarConversaoEmProcedimento(99L));
        Assert.That(ex!.Message, Does.Contain("já foi convertido"));
    }

    [Test]
    public void Total_SomaItensImplantesEquipeCirurgiasInternacaoAnestesia()
    {
        var orc = CriarOrcamento(
            itens: [new ItemPayload("Item", 1, 1000m, 0)],
            implantes: [new ImplantePayload(null, "Prótese", 1m, 500m)],
            equipe: [new EquipePayload(Guid.NewGuid(), "Cirurgião", 200m)]);

        // 1000 + 500 + 200 = 1700
        Assert.That(orc.Total, Is.EqualTo(1700m));
    }
}
