using Imedto.Backend.Application.Orcamentos.Catalogos;
using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Orcamentos.Catalogos.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Prontuarios.Pendencias;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

/// <summary>
/// Testes de unidade para MarcarProcedimentoRealizadoCommandHandler (F4 — briefing 2026-06-10_013).
/// Cobre: idempotência (CA77), tipo errado de pendência, evolução sem procedimentos (CA80),
/// multi-tenant (CA81/R11), vínculo CatalogoProduto (CA90-CA92).
/// A path de baixa de estoque (Dapper batched) é coberta por testes de integração.
/// </summary>
[TestFixture]
public class MarcarProcedimentoRealizadoCommandHandlerTests
{
    private const long EstabA = 1;
    private const long EstabB = 99;
    private const long PacienteId = 100;
    private const long EvolucaoId = 500;
    private static readonly Guid AutorId = Guid.NewGuid();

    // Conn string fake — não é usada nos paths testados antes do Dapper (idempotência/422 lançam antes).
    // Para o teste D1/D2/D6 que precisa avançar até Salvar(cobranca), a string precisa ser válida o
    // suficiente para não falhar na criação do NpgsqlConnection, mas o QueryAsync vai falhar na rede.
    private static readonly AppReadConnectionString FakeConn =
        new("Host=localhost;Port=5432;Database=fake_unit_test;Username=fake;Password=fake;");

    private static PendenciaAtendimento CriarPendenciaMarcar(long estabId = EstabA)
        => PendenciaAtendimento.Criar(estabId, PacienteId, EvolucaoId, null,
            AcaoPendencia.MarcarProcedimentoRealizado, AutorId);

    private static Prontuario CriarProntuario(long estabId = EstabA)
        => Prontuario.Iniciar(PacienteId, estabId, modeloDeProntuarioId: 1);

    private static ProntuarioEvolucao CriarEvolucao(string conteudoJson)
        => ProntuarioEvolucao.Registrar(
            prontuarioId: 10,
            autorUsuarioId: AutorId,
            modeloDeProntuarioIdOrigem: 1,
            modeloSnapshotJson: "{}",
            conteudoJson: conteudoJson);

    private static MarcarProcedimentoRealizadoCommandHandler CriarHandler(
        Mock<IPendenciaAtendimentoRepository> pendenciaRepo,
        Mock<IProntuarioRepository> prontuarioRepo,
        Mock<IProntuarioEvolucaoRepository> evolucaoRepo,
        Mock<ICobrancaRepository>? cobrancaRepo = null,
        Mock<IItemInventarioRepository>? invRepo = null,
        Mock<IMovimentacaoEstoqueRepository>? movRepo = null)
    {
        return new MarcarProcedimentoRealizadoCommandHandler(
            pendenciaRepo.Object,
            prontuarioRepo.Object,
            evolucaoRepo.Object,
            (cobrancaRepo ?? new Mock<ICobrancaRepository>()).Object,
            (invRepo ?? new Mock<IItemInventarioRepository>()).Object,
            (movRepo ?? new Mock<IMovimentacaoEstoqueRepository>()).Object,
            FakeConn);
    }

    // ── CA77: Idempotência — pendência já concluída retorna sem erro ───────────────

    [Test]
    public async Task Idempotencia_PendenciaJaConcluida_RetornaSemPersistencia_CA77()
    {
        var pendencia = CriarPendenciaMarcar();
        pendencia.ConcluirManualmente(); // simula conclusão anterior

        var pendenciaRepo = new Mock<IPendenciaAtendimentoRepository>();
        pendenciaRepo.Setup(r => r.ObterPorId(1L, EstabA)).ReturnsAsync(pendencia);
        var cobrancaRepo = new Mock<ICobrancaRepository>();

        var handler = CriarHandler(pendenciaRepo, new(), new(), cobrancaRepo);

        await handler.Handle(new MarcarProcedimentoRealizadoCommand
        {
            PendenciaId = 1,
            EstabelecimentoId = EstabA,
            UsuarioId = AutorId
        });

        // Não deve ter criado cobrança (no-op silencioso)
        cobrancaRepo.Verify(r => r.Salvar(It.IsAny<Cobranca>()), Times.Never,
            "Cobrança não deve ser criada se pendência já concluída.");
    }

    // ── Multi-tenant: pendência de outro tenant → 422 genérico ───────────────────

    [Test]
    public void MultiTenant_PendenciaOutroTenant_LancaBusinessException_CA81()
    {
        var pendenciaRepo = new Mock<IPendenciaAtendimentoRepository>();
        pendenciaRepo.Setup(r => r.ObterPorId(It.IsAny<long>(), EstabB))
                     .ReturnsAsync((PendenciaAtendimento?)null);

        var handler = CriarHandler(pendenciaRepo, new(), new());

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(new MarcarProcedimentoRealizadoCommand
            {
                PendenciaId = 1,
                EstabelecimentoId = EstabB,
                UsuarioId = AutorId
            }));

        // Mensagem genérica — não revela que registro pertence a outro tenant (LGPD)
        Assert.That(ex!.Message, Is.EqualTo("Pendência não encontrada."));
    }

    // ── Tipo de ação inválido ──────────────────────────────────────────────────────

    [Test]
    public void TipoAcaoErrado_LancaBusinessException()
    {
        var pendencia = PendenciaAtendimento.Criar(EstabA, PacienteId, EvolucaoId, null,
            AcaoPendencia.CriarReceita, AutorId);

        var pendenciaRepo = new Mock<IPendenciaAtendimentoRepository>();
        pendenciaRepo.Setup(r => r.ObterPorId(1L, EstabA)).ReturnsAsync(pendencia);

        var handler = CriarHandler(pendenciaRepo, new(), new());

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(new MarcarProcedimentoRealizadoCommand
            {
                PendenciaId = 1,
                EstabelecimentoId = EstabA,
                UsuarioId = AutorId
            }));

        Assert.That(ex!.Message, Does.Contain("Marcar procedimento realizado"));
    }

    // ── CA80: Evolução sem procedimentos-indicados lança 422 ─────────────────────

    [Test]
    public void SemProcedimentosIndicados_LancaBusinessException_CA80()
    {
        var pendencia = CriarPendenciaMarcar();
        var prontuario = CriarProntuario();
        var evolucao = CriarEvolucao("""{"conduta":{"observacao":"sem procedimentos"}}""");

        var pendenciaRepo = new Mock<IPendenciaAtendimentoRepository>();
        pendenciaRepo.Setup(r => r.ObterPorId(1L, EstabA)).ReturnsAsync(pendencia);

        var prontuarioRepo = new Mock<IProntuarioRepository>();
        prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabA)).ReturnsAsync(prontuario);

        var evolucaoRepo = new Mock<IProntuarioEvolucaoRepository>();
        evolucaoRepo.Setup(r => r.ObterDoProntuarioOuNulo(EvolucaoId, prontuario.Id)).ReturnsAsync(evolucao);

        var handler = CriarHandler(pendenciaRepo, prontuarioRepo, evolucaoRepo);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(new MarcarProcedimentoRealizadoCommand
            {
                PendenciaId = 1,
                EstabelecimentoId = EstabA,
                UsuarioId = AutorId
            }));

        Assert.That(ex!.Message, Does.Contain("não tem procedimentos indicados"));
    }

    [Test]
    public void ProcedimentosSemCatalogoCirurgiaId_TrataComoVazio_CA80()
    {
        // Itens legado texto-livre (sem catalogoCirurgiaId) são ignorados → lista vazia → 422
        var pendencia = CriarPendenciaMarcar();
        var prontuario = CriarProntuario();
        var evolucao = CriarEvolucao(
            """{"procedimentos-indicados":{"procedimentos":[{"descricao":"Consulta","valor":150}]}}""");

        var pendenciaRepo = new Mock<IPendenciaAtendimentoRepository>();
        pendenciaRepo.Setup(r => r.ObterPorId(1L, EstabA)).ReturnsAsync(pendencia);

        var prontuarioRepo = new Mock<IProntuarioRepository>();
        prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabA)).ReturnsAsync(prontuario);

        var evolucaoRepo = new Mock<IProntuarioEvolucaoRepository>();
        evolucaoRepo.Setup(r => r.ObterDoProntuarioOuNulo(EvolucaoId, prontuario.Id)).ReturnsAsync(evolucao);

        var handler = CriarHandler(pendenciaRepo, prontuarioRepo, evolucaoRepo);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(new MarcarProcedimentoRealizadoCommand
            {
                PendenciaId = 1,
                EstabelecimentoId = EstabA,
                UsuarioId = AutorId
            }));

        Assert.That(ex!.Message, Does.Contain("não tem procedimentos indicados"));
    }

    // ── Cobrança criada com atributos corretos (D1/D2/D6) — testada via domínio ─────
    //
    // Nota de design: o handler usa Dapper (AppReadConnectionString) para resolver produtos,
    // impossibilitando mock completo sem uma conexão real. O teste de atributos da cobrança
    // fica em CobrancaCriadaParaProcedimentoTests (domínio puro), que é mais preciso e rápido.
    // O path handler completo é coberto pelos testes de integração.
    //
    // Aqui testamos apenas que o handler NÃO cria cobrança quando o JSON é inválido (CA80),
    // e que a idempotência (CA77) e multi-tenant (CA81) funcionam — esses paths encerram
    // antes do Dapper e são testáveis em unidade.
}

/// <summary>
/// Testes de domínio: Cobranca.CriarParaProcedimento (F4 — D1/D2/D6).
/// </summary>
[TestFixture]
public class CobrancaCriadaParaProcedimentoTests
{
    [Test]
    public void CriarParaProcedimento_AtributosCorretos_D1_D2_D6()
    {
        var uid = Guid.NewGuid();
        var cobranca = Cobranca.CriarParaProcedimento(
            estabelecimentoId: 1,
            pacienteId: 200,
            evolucaoId: 500,
            agendamentoId: null,
            valorCobrado: 1500m,
            descricao: "Procedimento realizado: Cirurgia A",
            criadoPorUsuarioId: uid);

        Assert.That(cobranca.EstabelecimentoId, Is.EqualTo(1));
        Assert.That(cobranca.PacienteId, Is.EqualTo(200));
        Assert.That(cobranca.EvolucaoId, Is.EqualTo(500)); // D6
        Assert.That(cobranca.ValorCobrado, Is.EqualTo(1500m)); // D1
        Assert.That(cobranca.TipoAtendimento, Is.EqualTo(TipoAtendimento.Particular)); // D2
        Assert.That(cobranca.Origem, Is.EqualTo("Procedimento")); // D1
        Assert.That(cobranca.AgendamentoId, Is.Null);
    }

    [Test]
    public void CriarParaProcedimento_ComAgendamentoId_AgendamentoIdSetado()
    {
        var cobranca = Cobranca.CriarParaProcedimento(
            estabelecimentoId: 1, pacienteId: 200, evolucaoId: 500,
            agendamentoId: 77L, valorCobrado: 200m,
            descricao: "Proc", criadoPorUsuarioId: Guid.NewGuid());

        Assert.That(cobranca.AgendamentoId, Is.EqualTo(77L));
    }
}

/// <summary>
/// Testes do addendum F4: CatalogoProduto.ItemInventarioId (CA90–CA92).
/// </summary>
[TestFixture]
public class CatalogoProdutoItemInventarioTests
{
    private const long EstabA = 1;

    [Test]
    public void Criar_SemItemInventarioId_ItemInventarioIdNulo_CA90()
    {
        var produto = CatalogoProduto.Criar(EstabA, "Prótese A", null, 500m, false, TipoOrcamentoProduto.OPME);
        Assert.That(produto.ItemInventarioId, Is.Null);
    }

    [Test]
    public void Criar_ComItemInventarioId_PersisteVinculo_CA90()
    {
        var produto = CatalogoProduto.Criar(EstabA, "Prótese B", null, 500m, false,
            TipoOrcamentoProduto.OPME, itemInventarioId: 42L);
        Assert.That(produto.ItemInventarioId, Is.EqualTo(42L));
    }

    [Test]
    public void Atualizar_ComItemInventarioId_AtualizaVinculo_CA91()
    {
        var produto = CatalogoProduto.Criar(EstabA, "Prótese C", null, null, false, TipoOrcamentoProduto.OPME);
        produto.Atualizar("Prótese C", null, null, false, TipoOrcamentoProduto.OPME, itemInventarioId: 77L);
        Assert.That(produto.ItemInventarioId, Is.EqualTo(77L));
    }

    [Test]
    public void Atualizar_RemoveItemInventarioId_ViraNull()
    {
        var produto = CatalogoProduto.Criar(EstabA, "Prótese D", null, null, false,
            TipoOrcamentoProduto.OPME, itemInventarioId: 99L);
        produto.Atualizar("Prótese D", null, null, false, TipoOrcamentoProduto.OPME, itemInventarioId: null);
        Assert.That(produto.ItemInventarioId, Is.Null);
    }

    [Test]
    public void CriarHandler_ItemInventarioIdDeOutroTenant_LancaBusinessException_CA92()
    {
        var repo = new Mock<ICatalogoProdutoRepository>();
        var inv = new Mock<IItemInventarioRepository>();
        // Retorna null = item não pertence a este tenant
        inv.Setup(i => i.ObterPorIdOuNulo(42L, EstabA)).ReturnsAsync((ItemInventario?)null);

        var handler = new CriarCatalogoProdutoCommandHandler(repo.Object, inv.Object);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(new CriarCatalogoProdutoCommand
            {
                EstabelecimentoId = EstabA,
                Nome = "Produto X",
                Tipo = "OPME",
                ItemInventarioId = 42L
            }));

        // Mensagem genérica — não revela que item pertence a outro tenant (LGPD/multi-tenant)
        Assert.That(ex!.Message, Is.EqualTo("Não encontrado."));
    }

    [Test]
    public void AtualizarHandler_ItemInventarioIdDeOutroTenant_LancaBusinessException_CA92()
    {
        var produto = CatalogoProduto.Criar(EstabA, "Prod Y", null, null, false, TipoOrcamentoProduto.OPME);
        var repo = new Mock<ICatalogoProdutoRepository>();
        repo.Setup(r => r.ObterPorIdOuNulo(10L, EstabA)).ReturnsAsync(produto);

        var inv = new Mock<IItemInventarioRepository>();
        inv.Setup(i => i.ObterPorIdOuNulo(55L, EstabA)).ReturnsAsync((ItemInventario?)null);

        var handler = new AtualizarCatalogoProdutoCommandHandler(repo.Object, inv.Object);

        Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(new AtualizarCatalogoProdutoCommand
            {
                Id = 10L,
                EstabelecimentoId = EstabA,
                Nome = "Prod Y",
                Tipo = "OPME",
                ItemInventarioId = 55L
            }));
    }

    [Test]
    public async Task CriarHandler_ComItemInventarioIdValido_PersisteProdutoComVinculo_CA90()
    {
        // Cria item com todos os campos obrigatórios
        var invItem = ItemInventario.Criar(
            estabelecimentoId: EstabA,
            codigo: "ITEM-01",
            nome: "Prótese Mamária 250cc",
            categoriaId: 1,
            categoriaNomeSnapshot: "OPME",
            unidadeMedida: "un",
            quantidadeMinima: 0m,
            fabricanteId: null,
            fornecedorPadraoId: null,
            localPadraoId: null,
            custoUnitario: 500m);

        var repo = new Mock<ICatalogoProdutoRepository>();
        var inv = new Mock<IItemInventarioRepository>();
        inv.Setup(i => i.ObterPorIdOuNulo(42L, EstabA)).ReturnsAsync(invItem);

        var handler = new CriarCatalogoProdutoCommandHandler(repo.Object, inv.Object);

        await handler.Handle(new CriarCatalogoProdutoCommand
        {
            EstabelecimentoId = EstabA,
            Nome = "Prótese X",
            Tipo = "OPME",
            ItemInventarioId = 42L
        });

        repo.Verify(r => r.Salvar(It.Is<CatalogoProduto>(p =>
            p.ItemInventarioId == 42L &&
            p.EstabelecimentoId == EstabA)), Times.Once);
    }
}
