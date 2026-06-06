using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Imedto.Backend.Test.Application.Prontuarios;

/// <summary>
/// Testes de unidade do <see cref="PoolExtratorEvolucao"/>.
/// Cobre: CA2, CA3, CA4, CA5, CA6, CA8, CA9, CA16.
/// </summary>
[TestFixture]
public class PoolExtratorEvolucaoTests
{
    private Mock<IProntuarioVariavelPoolRepository> _repo;
    private PoolExtratorEvolucao _sut;

    private const long EstabelecimentoId = 1;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IProntuarioVariavelPoolRepository>();
        _repo.Setup(r => r.ListarAtivosPorTipo(It.IsAny<long>(), It.IsAny<TipoVariavelPool>()))
             .ReturnsAsync(new List<ProntuarioVariavelPool>());
        _repo.Setup(r => r.Salvar(It.IsAny<ProntuarioVariavelPool>()))
             .Returns(Task.CompletedTask);
        _sut = new PoolExtratorEvolucao(_repo.Object);
    }

    // ── CA2: criação automática ao salvar ───────────────────────────────────────

    [Test]
    public async Task ExtrairECriar_AlergiaInedita_CriaItemNoPool()
    {
        var json = """{"hpp":{"alergias":[{"nome":"Sulfa","observacao":"grave"}]}}""";

        await _sut.ExtrairECriar(EstabelecimentoId, json);

        _repo.Verify(r => r.Salvar(It.Is<ProntuarioVariavelPool>(i =>
            i.Tipo == TipoVariavelPool.Alergia &&
            i.Nome == "Sulfa" &&
            i.EstabelecimentoId == EstabelecimentoId &&
            i.EhPadraoSistema == false &&
            i.Ativo == true)),
            Times.Once);
    }

    [Test]
    public async Task ExtrairECriar_MedicamentoInedito_CriaItemNoPool()
    {
        var json = """{"hpp":{"medicacoes":[{"nome":"Losartana","dose":"50mg","frequencia":"1x/dia","motivo":"HAS","observacoes":""}]}}""";

        await _sut.ExtrairECriar(EstabelecimentoId, json);

        // CA8: só 'nome' vira pool — dose/motivo/observacoes ignorados
        _repo.Verify(r => r.Salvar(It.Is<ProntuarioVariavelPool>(i =>
            i.Tipo == TipoVariavelPool.Medicamento &&
            i.Nome == "Losartana")),
            Times.Once);
        // Confirma que apenas 1 item foi criado (não cria para dose, motivo etc.)
        _repo.Verify(r => r.Salvar(It.IsAny<ProntuarioVariavelPool>()), Times.Once);
    }

    [Test]
    public async Task ExtrairECriar_RelacaoFamiliarInedita_CriaItemNoPool()
    {
        var json = """{"h-familiar":{"parentes":[{"parentesco":"Primo(a)","doencas":"Diabetes","comentario":""}]}}""";

        await _sut.ExtrairECriar(EstabelecimentoId, json);

        _repo.Verify(r => r.Salvar(It.Is<ProntuarioVariavelPool>(i =>
            i.Tipo == TipoVariavelPool.RelacaoFamiliar &&
            i.Nome == "Primo(a)")),
            Times.Once);
    }

    // ── CA3: dedup case/acento/trim ─────────────────────────────────────────────

    [Test]
    public async Task ExtrairECriar_NomeComAcentoEEspacos_NaoDuplicaExistente()
    {
        // "Hipertensão" já existe no pool
        var existente = CriarItemExistente(TipoVariavelPool.Doenca, "Hipertensão");
        _repo.Setup(r => r.ListarAtivosPorTipo(EstabelecimentoId, TipoVariavelPool.Doenca))
             .ReturnsAsync(new List<ProntuarioVariavelPool> { existente });

        // Chegou " hipertensao " (trim + lower + sem acento) na evolução
        var json = """{"hpp":{"doencas":[{"nome":" hipertensao ","observacao":""}]}}""";

        await _sut.ExtrairECriar(EstabelecimentoId, json);

        _repo.Verify(r => r.Salvar(It.IsAny<ProntuarioVariavelPool>()), Times.Never);
    }

    [Test]
    public async Task ExtrairECriar_NomeCaseDiferente_NaoDuplicaExistente()
    {
        var existente = CriarItemExistente(TipoVariavelPool.Alergia, "Penicilina");
        _repo.Setup(r => r.ListarAtivosPorTipo(EstabelecimentoId, TipoVariavelPool.Alergia))
             .ReturnsAsync(new List<ProntuarioVariavelPool> { existente });

        var json = """{"hpp":{"alergias":[{"nome":"PENICILINA","observacao":""}]}}""";

        await _sut.ExtrairECriar(EstabelecimentoId, json);

        _repo.Verify(r => r.Salvar(It.IsAny<ProntuarioVariavelPool>()), Times.Never);
    }

    // ── CA4: dedup reusa padrão-sistema sem copiar ──────────────────────────────

    [Test]
    public async Task ExtrairECriar_ItemPadraoSistema_NaoCopiaParaEstabelecimento()
    {
        var padrao = CriarPadraoSistema(TipoVariavelPool.Cirurgia, "Apendicectomia");
        _repo.Setup(r => r.ListarAtivosPorTipo(EstabelecimentoId, TipoVariavelPool.Cirurgia))
             .ReturnsAsync(new List<ProntuarioVariavelPool> { padrao });

        var json = """{"hpp":{"cirurgias":[{"nome":"apendicectomia","ano":"2020","observacao":""}]}}""";

        await _sut.ExtrairECriar(EstabelecimentoId, json);

        _repo.Verify(r => r.Salvar(It.IsAny<ProntuarioVariavelPool>()), Times.Never);
    }

    // ── CA9: campos vazios não geram lixo ──────────────────────────────────────

    [Test]
    public async Task ExtrairECriar_NomeVazioOuEspacos_NaoCriaItem()
    {
        var json = """{"hpp":{"alergias":[{"nome":"","observacao":""},{"nome":"   ","observacao":""}]}}""";

        await _sut.ExtrairECriar(EstabelecimentoId, json);

        _repo.Verify(r => r.Salvar(It.IsAny<ProntuarioVariavelPool>()), Times.Never);
    }

    // ── CA8: LGPD — campos livres nunca viram pool ──────────────────────────────

    [Test]
    public async Task ExtrairECriar_MedicacaoComCamposLivres_SoCriaNome()
    {
        // Simula evolução com múltiplos campos preenchidos
        var json = """{"hpp":{"medicacoes":[{"nome":"Metformina","dose":"850mg","frequencia":"2x/dia","motivo":"Diabetes tipo 2","observacoes":"Tomar após refeição"}]}}""";

        await _sut.ExtrairECriar(EstabelecimentoId, json);

        // Apenas 1 item criado (Metformina) — dose, motivo, observacoes não viram pool
        _repo.Verify(r => r.Salvar(It.IsAny<ProntuarioVariavelPool>()), Times.Once);
        _repo.Verify(r => r.Salvar(It.Is<ProntuarioVariavelPool>(i => i.Nome == "Metformina")), Times.Once);
    }

    // ── CA9 / dedup interno: duplicata dentro do mesmo JSON não cria 2x ─────────

    [Test]
    public async Task ExtrairECriar_MesmoNomeDuplicadoNoArray_CriaSoUmaVez()
    {
        var json = """{"hpp":{"alergias":[{"nome":"Látex"},{"nome":"látex"},{"nome":" LATEX "}]}}""";

        await _sut.ExtrairECriar(EstabelecimentoId, json);

        _repo.Verify(r => r.Salvar(It.IsAny<ProntuarioVariavelPool>()), Times.Once);
    }

    // ── CA11/CA16: JSON inválido não quebra o fluxo ─────────────────────────────

    [Test]
    public async Task ExtrairECriar_JsonInvalido_NaoLancaExcecao()
    {
        // Não deve lançar — falha-suave
        Assert.DoesNotThrowAsync(() => _sut.ExtrairECriar(EstabelecimentoId, "INVALIDO{"));
    }

    [Test]
    public async Task ExtrairECriar_JsonSemChaveHpp_NaoCriaItens()
    {
        var json = """{"queixa":"dor de cabeça","observacao":"frequente"}""";

        await _sut.ExtrairECriar(EstabelecimentoId, json);

        _repo.Verify(r => r.Salvar(It.IsAny<ProntuarioVariavelPool>()), Times.Never);
    }

    // ── CA5: isolamento multi-tenant ────────────────────────────────────────────

    [Test]
    public async Task ExtrairECriar_UsaEstabelecimentoIdDoCommand()
    {
        long tenantCorreto = 99;
        var json = """{"hpp":{"alergias":[{"nome":"Abelha"}]}}""";

        await _sut.ExtrairECriar(tenantCorreto, json);

        _repo.Verify(r => r.Salvar(It.Is<ProntuarioVariavelPool>(i =>
            i.EstabelecimentoId == tenantCorreto)),
            Times.Once);
    }

    // ── Extração de todos os 5 tipos ────────────────────────────────────────────

    [Test]
    public async Task ExtrairECriar_TodosOsTiposMapeados_CriaUmItemPorTipo()
    {
        var json = """
        {
            "hpp": {
                "alergias":   [{"nome":"Alergia1"}],
                "medicacoes": [{"nome":"Med1"}],
                "cirurgias":  [{"nome":"Cir1"}],
                "doencas":    [{"nome":"Doenca1"}]
            },
            "h-familiar": {
                "parentes": [{"parentesco":"Parente1","doencas":"","comentario":""}]
            }
        }
        """;

        await _sut.ExtrairECriar(EstabelecimentoId, json);

        _repo.Verify(r => r.Salvar(It.Is<ProntuarioVariavelPool>(i => i.Tipo == TipoVariavelPool.Alergia)), Times.Once);
        _repo.Verify(r => r.Salvar(It.Is<ProntuarioVariavelPool>(i => i.Tipo == TipoVariavelPool.Medicamento)), Times.Once);
        _repo.Verify(r => r.Salvar(It.Is<ProntuarioVariavelPool>(i => i.Tipo == TipoVariavelPool.Cirurgia)), Times.Once);
        _repo.Verify(r => r.Salvar(It.Is<ProntuarioVariavelPool>(i => i.Tipo == TipoVariavelPool.Doenca)), Times.Once);
        _repo.Verify(r => r.Salvar(It.Is<ProntuarioVariavelPool>(i => i.Tipo == TipoVariavelPool.RelacaoFamiliar)), Times.Once);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static ProntuarioVariavelPool CriarItemExistente(TipoVariavelPool tipo, string nome)
    {
        var item = ProntuarioVariavelPool.CriarDoEstabelecimento(EstabelecimentoId, tipo, nome);
        typeof(Imedto.Backend.SharedKernel.Domain.Entity)
            .GetProperty(nameof(Imedto.Backend.SharedKernel.Domain.Entity.Id))!
            .SetValue(item, 1L);
        return item;
    }

    private static ProntuarioVariavelPool CriarPadraoSistema(TipoVariavelPool tipo, string nome)
    {
        var item = ProntuarioVariavelPool.CriarPadraoSistema(tipo, nome);
        typeof(Imedto.Backend.SharedKernel.Domain.Entity)
            .GetProperty(nameof(Imedto.Backend.SharedKernel.Domain.Entity.Id))!
            .SetValue(item, 2L);
        return item;
    }
}
