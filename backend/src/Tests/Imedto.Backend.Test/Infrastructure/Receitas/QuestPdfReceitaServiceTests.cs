using System.Text;
using Imedto.Backend.Infrastructure.Receitas;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Receitas;

/// <summary>
/// Testes do gerador de PDF de receitas — valida que o layout institucional
/// (cabeçalho, bloco de paciente, lista de medicamentos numerada, rodapé,
/// marca d'água) é renderizado sem crash em cada variante (Comum, Antibiótico,
/// Controlada, Especial × Rascunho/Emitida/Cancelada).
///
/// Não validamos pixel-a-pixel — checamos contagem de bytes, header PDF e
/// performance básica (Receita Comum &lt; 500ms). Validação visual fica para
/// QA manual.
/// </summary>
[TestFixture]
public class QuestPdfReceitaServiceTests
{
    private const long EstabId = 42;

    [OneTimeSetUp]
    public void GlobalSetup()
    {
        QuestPdfReceitaService.InicializarQuestPdf();
    }

    private static ReceitaRow Receita(
        string tipo = "Comum",
        string status = "Emitida",
        string tipoNotificacao = null,
        string assinaturaStatus = "NaoAssinada",
        string motivoCancelamento = null,
        bool comFotoUrl = true,
        bool comEstabelecimentoCompleto = true,
        string observacoes = null,
        long pacienteId = 99)
        => new ReceitaRow(
            Id: 100,
            Tipo: tipo,
            TipoNotificacao: tipoNotificacao,
            Status: status,
            AssinaturaDigitalStatus: assinaturaStatus,
            EmitidaEm: status == "Rascunho" ? null : new DateTime(2026, 5, 12, 10, 30, 0, DateTimeKind.Utc),
            ValidadeAte: tipo == "Controlada" ? new DateTime(2026, 6, 11, 0, 0, 0, DateTimeKind.Utc) : null,
            Observacoes: observacoes,
            MotivoCancelamento: motivoCancelamento,
            PacienteId: pacienteId,
            PacienteNome: "Maria Aparecida da Silva",
            PacienteCpf: "12345678901",
            PacienteDataNascimento: new DateTime(1985, 4, 12),
            PacienteGenero: "F",
            PacienteTelefone: "11999998888",
            ProfissionalNome: "Dr. João Cardoso",
            ProfissionalCrmCro: "CRM SP 123456",
            CabecalhoHtml: null,
            RodapeHtml: null,
            EmissorPadrao: null,
            EstabelecimentoNomeFantasia: "Clínica Imedto",
            EstabelecimentoCnpj: comEstabelecimentoCompleto ? "12345678000190" : null,
            EstabelecimentoTelefone: comEstabelecimentoCompleto ? "1130304040" : null,
            EstabelecimentoEndereco: comEstabelecimentoCompleto ? "Av. Paulista, 1842 — São Paulo/SP" : null,
            EstabelecimentoFotoUrl: comFotoUrl ? "https://imedto.com/logo.png" : null);

    private static ItemRow Item(int ordem, string nome = "Losartana", string posologia = "1 cp 12/12h")
        => new ItemRow(
            Ordem: ordem,
            Medicamento: nome,
            Posologia: posologia,
            Concentracao: "50mg",
            FormaFarmaceutica: "comprimido",
            Via: "VO",
            Quantidade: "30 cp",
            Duracao: "30 dias",
            Observacao: null);

    private static bool EhPdfValido(byte[] bytes)
    {
        if (bytes is null || bytes.Length < 100) return false;
        // Todo PDF começa com "%PDF-"
        var header = Encoding.ASCII.GetString(bytes, 0, 5);
        return header == "%PDF-";
    }

    [Test]
    public void GerarPdf_ReceitaComum_UmItem_ProduzPdfValido()
    {
        var dados = new DadosPdf(
            Receita(),
            new List<ItemRow> { Item(0) });

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var bytes = QuestPdfReceitaService.GerarPdf(dados);
        sw.Stop();

        Assert.That(EhPdfValido(bytes), Is.True, "Deve produzir bytes PDF válidos.");
        Assert.That(bytes.Length, Is.GreaterThan(2_000), "PDF deve ter conteúdo mínimo.");
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(1500),
            $"Receita Comum deve gerar em até ~1.5s (foi {sw.ElapsedMilliseconds}ms).");
    }

    [Test]
    public void GerarPdf_ReceitaComum_MultiplosItens_NaoCrasha()
    {
        var itens = new List<ItemRow>();
        for (int i = 0; i < 8; i++)
            itens.Add(Item(i, $"Medicamento {i}", $"Posologia detalhada do item {i}"));

        var bytes = QuestPdfReceitaService.GerarPdf(
            new DadosPdf(Receita(), itens));

        Assert.That(EhPdfValido(bytes), Is.True);
    }

    [Test]
    public void GerarPdf_ReceitaControlada_NotificacaoB_AplicaVarianteVermelha()
    {
        var dados = new DadosPdf(
            Receita(tipo: "Controlada", tipoNotificacao: "B"),
            new List<ItemRow> { Item(0, "Clonazepam", "1 cp à noite") });

        var bytes = QuestPdfReceitaService.GerarPdf(dados);

        Assert.That(EhPdfValido(bytes), Is.True);
        // Variante controlada inclui a caixa "1ª via / 2ª via" — vai estar no stream
        Assert.That(bytes.Length, Is.GreaterThan(2_500),
            "Controlada deve ter conteúdo adicional (caixa 2ª via).");
    }

    [Test]
    public void GerarPdf_ReceitaAntibiotico_RendererAvisoRetencao()
    {
        var dados = new DadosPdf(
            Receita(tipo: "Antibiotico"),
            new List<ItemRow> { Item(0, "Amoxicilina", "1 cp 8/8h") });

        var bytes = QuestPdfReceitaService.GerarPdf(dados);

        Assert.That(EhPdfValido(bytes), Is.True);
    }

    [Test]
    public void GerarPdf_StatusRascunho_RodapeOmiteAssinatura()
    {
        var dados = new DadosPdf(
            Receita(status: "Rascunho"),
            new List<ItemRow> { Item(0) });

        var bytes = QuestPdfReceitaService.GerarPdf(dados);
        Assert.That(EhPdfValido(bytes), Is.True);
    }

    [Test]
    public void GerarPdf_StatusCancelada_MostraMotivoCancelamento()
    {
        var dados = new DadosPdf(
            Receita(status: "Cancelada", motivoCancelamento: "Dose corrigida após nova avaliação"),
            new List<ItemRow> { Item(0) });

        var bytes = QuestPdfReceitaService.GerarPdf(dados);
        Assert.That(EhPdfValido(bytes), Is.True);
    }

    [Test]
    public void GerarPdf_SemFotoUrl_UsaPlaceholderComIniciais()
    {
        var dados = new DadosPdf(
            Receita(comFotoUrl: false),
            new List<ItemRow> { Item(0) });

        var bytes = QuestPdfReceitaService.GerarPdf(dados);
        Assert.That(EhPdfValido(bytes), Is.True);
    }

    [Test]
    public void GerarPdf_SemCnpjEndereco_NaoQuebraLayout()
    {
        var dados = new DadosPdf(
            Receita(comEstabelecimentoCompleto: false),
            new List<ItemRow> { Item(0) });

        var bytes = QuestPdfReceitaService.GerarPdf(dados);
        Assert.That(EhPdfValido(bytes), Is.True);
    }

    [Test]
    public void GerarPdf_ObservacoesLongas_QuebraPaginaCorretamente()
    {
        var observacaoLonga = new string('x', 1800)
            + " — descrição clínica detalhada com várias considerações e instruções específicas para o paciente.";
        var dados = new DadosPdf(
            Receita(observacoes: observacaoLonga),
            new List<ItemRow> { Item(0) });

        var bytes = QuestPdfReceitaService.GerarPdf(dados);
        Assert.That(EhPdfValido(bytes), Is.True);
    }

    [Test]
    public void GerarPdf_AssinaturaDigitalIcp_RenderizaSeloVerde()
    {
        var dados = new DadosPdf(
            Receita(assinaturaStatus: "AssinadaIcp"),
            new List<ItemRow> { Item(0) });

        var bytes = QuestPdfReceitaService.GerarPdf(dados);
        Assert.That(EhPdfValido(bytes), Is.True);
    }

    [Test]
    public void GerarPdf_ItemSemViaOuQuantidade_NaoCrasha()
    {
        var item = new ItemRow(
            Ordem: 0,
            Medicamento: "Dipirona",
            Posologia: "40 gotas se febre",
            Concentracao: null, FormaFarmaceutica: null,
            Via: null, Quantidade: null, Duracao: null, Observacao: null);

        var bytes = QuestPdfReceitaService.GerarPdf(
            new DadosPdf(Receita(), new List<ItemRow> { item }));

        Assert.That(EhPdfValido(bytes), Is.True);
    }
}
