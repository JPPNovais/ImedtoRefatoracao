using System.Text.Json;
using Imedto.Backend.Application.Migracao.Jobs;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Migracao;

/// <summary>
/// Testes do CarregarOnda1JobHandler (briefing 2026-06-15_001 — Marco 3).
/// Cobrem: CA22 (sem job não faz nada), CA23 (lote 100, FK-safe order),
/// CA25/CA26 (concluido vs concluido_com_erros), multi-tenant (CA2), entidade desconhecida → pulado.
/// </summary>
[TestFixture]
public class CarregarOnda1JobHandlerTests
{
    private Mock<IMigracaoJobRepository>           _jobRepo;
    private Mock<IMigracaoRegistroRepository>      _registroRepo;
    private Mock<IMigracaoJobEventoRepository>     _eventoRepo;
    private Mock<IPacienteRepository>              _pacienteRepo;
    private Mock<ICategoriaEstoqueRepository>      _categoriaRepo;
    private Mock<IFabricanteEstoqueRepository>     _fabricanteRepo;
    private Mock<IFornecedorEstoqueRepository>     _fornecedorRepo;
    private Mock<ILocalEstoqueRepository>          _localRepo;
    private Mock<IItemInventarioRepository>        _itemRepo;
    private Mock<IAgendamentoRepository>           _agendamentoRepo;
    private Mock<ICatalogoCirurgiaRepository>      _cirurgiaRepo;
    private Mock<ICatalogoProdutoRepository>       _produtoRepo;
    private Mock<IMigracaoCatalogoCirurgiaLookup>  _cirurgiaLookup;
    private Mock<IMigracaoCatalogoProdutoLookup>   _produtoLookup;
    private Mock<IMigracaoPacienteLookup>          _pacienteLookup;
    private Mock<IMigracaoAgendamentoLookup>       _agendamentoLookup;

    private CarregarOnda1JobHandler _sut;

    private const long EstabelecimentoId = 42;
    private const long JobId = 99;

    [SetUp]
    public void SetUp()
    {
        _jobRepo        = new Mock<IMigracaoJobRepository>();
        _registroRepo   = new Mock<IMigracaoRegistroRepository>();
        _eventoRepo     = new Mock<IMigracaoJobEventoRepository>();
        _eventoRepo.Setup(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);
        _pacienteRepo   = new Mock<IPacienteRepository>();
        _categoriaRepo  = new Mock<ICategoriaEstoqueRepository>();
        _fabricanteRepo = new Mock<IFabricanteEstoqueRepository>();
        _fornecedorRepo = new Mock<IFornecedorEstoqueRepository>();
        _localRepo      = new Mock<ILocalEstoqueRepository>();
        _itemRepo       = new Mock<IItemInventarioRepository>();
        _agendamentoRepo = new Mock<IAgendamentoRepository>();
        _cirurgiaRepo   = new Mock<ICatalogoCirurgiaRepository>();
        _produtoRepo    = new Mock<ICatalogoProdutoRepository>();
        _cirurgiaLookup    = new Mock<IMigracaoCatalogoCirurgiaLookup>();
        _produtoLookup     = new Mock<IMigracaoCatalogoProdutoLookup>();
        _pacienteLookup    = new Mock<IMigracaoPacienteLookup>();
        _agendamentoLookup = new Mock<IMigracaoAgendamentoLookup>();

        _sut = CriarSut();
    }

    private CarregarOnda1JobHandler CriarSut() => new(
        _jobRepo.Object,
        _registroRepo.Object,
        _eventoRepo.Object,
        _pacienteRepo.Object,
        _categoriaRepo.Object,
        _fabricanteRepo.Object,
        _fornecedorRepo.Object,
        _localRepo.Object,
        _itemRepo.Object,
        _agendamentoRepo.Object,
        _cirurgiaRepo.Object,
        _produtoRepo.Object,
        _cirurgiaLookup.Object,
        _produtoLookup.Object,
        _pacienteLookup.Object,
        _agendamentoLookup.Object,
        NullLogger<CarregarOnda1JobHandler>.Instance);

    private MigracaoJob CriarJobMigrando()
    {
        var adminId = Guid.NewGuid();
        var job = MigracaoJob.Criar(EstabelecimentoId, Guid.NewGuid(), "iClinic");
        // Addendum 003: upload → aguardando_aprovacao; AprovarAnalise → aguardando_mapa.
        job.RegistrarArquivoRecebido("migracao/42/99/arquivo.zip");
        job.AprovarAnalise(adminId);
        job.MarcarMapaEmRevisao();
        job.MarcarPreviewPronto(adminId);
        job.MarcarMigrando(adminId);
        return job;
    }

    private static MigracaoRegistro CriarRegistro(string entidade, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return MigracaoRegistro.Criar(JobId, EstabelecimentoId, entidade, json);
    }

    // ─── CA22 — sem job, não faz nada ───────────────────────────────────────────

    [Test]
    public async Task ExecutarAsync_QuandoNenhumJobMigrando_NaoFazNada()
    {
        // Arrange
        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOuNulo(default)).ReturnsAsync((MigracaoJob?)null);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        _registroRepo.Verify(r => r.ListarPorJob(It.IsAny<long>(), default), Times.Never);
        _jobRepo.Verify(r => r.Salvar(It.IsAny<MigracaoJob>(), default), Times.Never);
    }

    // ─── CA25 — sem pendentes → concluido ────────────────────────────────────────

    [Test]
    public async Task ExecutarAsync_QuandoNenhumRegistroPendente_MarcaJobConcluido()
    {
        // Arrange
        var job = CriarJobMigrando();
        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOuNulo(default)).ReturnsAsync(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default))
                     .ReturnsAsync(new List<MigracaoRegistro>());
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        Assert.That(job.Status, Is.EqualTo("concluido"));
        _jobRepo.Verify(r => r.Salvar(job, default), Times.Once);
    }

    // ─── CA26 — todos importados → concluido ─────────────────────────────────────

    [Test]
    public async Task ExecutarAsync_QuandoTodosImportados_MarcaConcluido()
    {
        // Arrange
        var job = CriarJobMigrando();
        var reg = CriarRegistro("categoria_estoque", new { nome = "Cirúrgico" });

        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOuNulo(default)).ReturnsAsync(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default))
                     .ReturnsAsync(new List<MigracaoRegistro> { reg });

        // Categoria não existe → será criada
        _categoriaRepo.Setup(r => r.ObterPorNomeOuNulo("Cirúrgico", EstabelecimentoId))
                      .ReturnsAsync((CategoriaEstoque?)null);
        _categoriaRepo.Setup(r => r.Salvar(It.IsAny<CategoriaEstoque>())).Returns(Task.CompletedTask);
        _registroRepo.Setup(r => r.Salvar(It.IsAny<MigracaoRegistro>(), default)).Returns(Task.CompletedTask);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        Assert.That(job.Status, Is.EqualTo("concluido"), "Todos importados → deve ser 'concluido'.");
        Assert.That(reg.Status, Is.EqualTo("importado_criado"), "Registro deve ficar como importado_criado.");
    }

    // ─── CA26 — algum rejeitado → concluido_com_erros ────────────────────────────

    [Test]
    public async Task ExecutarAsync_QuandoHaRegistroRejeitado_MarcaConcluidoComErros()
    {
        // Arrange
        var job = CriarJobMigrando();
        // Payload sem "nome" → fornecedor sem razao_social → rejeitado
        var reg = CriarRegistro("fornecedor_estoque", new { email = "teste@test.com" });

        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOuNulo(default)).ReturnsAsync(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default))
                     .ReturnsAsync(new List<MigracaoRegistro> { reg });
        _registroRepo.Setup(r => r.Salvar(It.IsAny<MigracaoRegistro>(), default)).Returns(Task.CompletedTask);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        Assert.That(job.Status, Is.EqualTo("concluido_com_erros"),
            "Rejeição deve resultar em 'concluido_com_erros'.");
        Assert.That(reg.Status, Is.EqualTo("rejeitado"));
        Assert.That(reg.MotivoRejeicao, Is.EqualTo("identificador ausente"),
            "Motivo de rejeição sem PII (CA4).");
    }

    // ─── Entidade desconhecida → pulado ──────────────────────────────────────────

    [Test]
    public async Task ExecutarAsync_QuandoEntidadeDesconhecida_MarcaRegistroPulado()
    {
        // Arrange
        var job = CriarJobMigrando();
        var reg = CriarRegistro("entidade_inexistente_onda2", new { id = "1" });

        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOuNulo(default)).ReturnsAsync(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default))
                     .ReturnsAsync(new List<MigracaoRegistro> { reg });
        _registroRepo.Setup(r => r.Salvar(It.IsAny<MigracaoRegistro>(), default)).Returns(Task.CompletedTask);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        Assert.That(reg.Status, Is.EqualTo("pulado"));
        Assert.That(reg.MotivoRejeicao, Is.EqualTo("entidade não suportada nesta onda"));
        // Entidade pulada não conta como rejeitado → job concluído (não com erros)
        Assert.That(job.Status, Is.EqualTo("concluido"));
    }

    // ─── Multi-tenant (CA2) — registros apenas do estabelecimento do job ──────────

    [Test]
    public async Task ExecutarAsync_UsaEstabelecimentoIdDoJob_EmTodasAsConsultas()
    {
        // Arrange
        var job = CriarJobMigrando();
        var reg = CriarRegistro("categoria_estoque", new { nome = "Cirúrgico" });

        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOuNulo(default)).ReturnsAsync(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default))
                     .ReturnsAsync(new List<MigracaoRegistro> { reg });
        _categoriaRepo.Setup(r => r.ObterPorNomeOuNulo(It.IsAny<string>(), EstabelecimentoId))
                      .ReturnsAsync((CategoriaEstoque?)null);
        _categoriaRepo.Setup(r => r.Salvar(It.IsAny<CategoriaEstoque>())).Returns(Task.CompletedTask);
        _registroRepo.Setup(r => r.Salvar(It.IsAny<MigracaoRegistro>(), default)).Returns(Task.CompletedTask);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert — a busca de existente usou o EstabelecimentoId correto do job (não outro tenant)
        _categoriaRepo.Verify(
            r => r.ObterPorNomeOuNulo("Cirúrgico", EstabelecimentoId),
            Times.Once,
            "Busca de existente deve filtrar pelo EstabelecimentoId do job.");
    }

    // ─── CA8 — item_estoque sem código e nome duplicado → rejeitado ──────────────

    [Test]
    public async Task ItemEstoque_SemCodigo_NomeDuplicado_DeveRejeitar()
    {
        // CA8: sem campo código, item com mesmo nome mas código diferente → "sem chave única para dedupe"
        var job = CriarJobMigrando();
        var nomeItem = "Luva cirúrgica P";
        // payload com categoria_nome preenchida, mas SEM codigo
        var reg = CriarRegistro("item_estoque", new { nome = nomeItem, categoria_nome = "Materiais" });

        var categoriaExistente = CategoriaEstoque.Criar(EstabelecimentoId, "Materiais", "hsl(218 70% 50%)", "fa-tag");
        // Item existente com mesmo nome mas código diferente → duplicação detectada
        var itemExistente = ItemInventario.Criar(
            EstabelecimentoId, "COD-LUVA-001", nomeItem,
            categoriaId: 1, categoriaNomeSnapshot: "Materiais", unidadeMedida: "un",
            quantidadeMinima: 0, fabricanteId: null, fornecedorPadraoId: null,
            localPadraoId: null, custoUnitario: null);

        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOuNulo(default)).ReturnsAsync(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default))
                     .ReturnsAsync(new List<MigracaoRegistro> { reg });
        _categoriaRepo.Setup(r => r.ObterPorNomeOuNulo("Materiais", EstabelecimentoId))
                      .ReturnsAsync(categoriaExistente);
        _itemRepo.Setup(r => r.ObterPorNomeOuNulo(nomeItem, EstabelecimentoId))
                 .ReturnsAsync(itemExistente);
        _registroRepo.Setup(r => r.Salvar(It.IsAny<MigracaoRegistro>(), default)).Returns(Task.CompletedTask);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        Assert.That(reg.Status, Is.EqualTo("rejeitado"), "Item sem código com nome duplicado deve ser rejeitado.");
        Assert.That(reg.MotivoRejeicao, Does.Contain("chave única").IgnoreCase,
            "Motivo deve mencionar falta de chave única (CA8).");
        Assert.That(job.Status, Is.EqualTo("concluido_com_erros"));
    }

    // ─── CA11 — item_estoque sem categoria → rejeitado (FK obrigatória) ──────────

    [Test]
    public async Task ItemEstoque_SemCategoria_DeveRejeitar()
    {
        // CA11: categoria é FK obrigatória para item de estoque
        var job = CriarJobMigrando();
        // sem campo categoria_nome → deve rejeitar
        var reg = CriarRegistro("item_estoque", new { codigo = "ITEM-001", nome = "Seringa 5ml" });

        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOuNulo(default)).ReturnsAsync(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default))
                     .ReturnsAsync(new List<MigracaoRegistro> { reg });
        _itemRepo.Setup(r => r.ObterPorCodigoOuNulo("ITEM-001", EstabelecimentoId))
                 .ReturnsAsync((ItemInventario?)null);
        _registroRepo.Setup(r => r.Salvar(It.IsAny<MigracaoRegistro>(), default)).Returns(Task.CompletedTask);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        Assert.That(reg.Status, Is.EqualTo("rejeitado"), "Item sem categoria deve ser rejeitado.");
        Assert.That(reg.MotivoRejeicao, Does.Contain("categoria").IgnoreCase,
            "Motivo deve mencionar a FK obrigatória ausente (CA11).");
        Assert.That(job.Status, Is.EqualTo("concluido_com_erros"));
    }

    // ─── CA11 — item_estoque sem fornecedor → criado sem FK opcional ─────────────

    [Test]
    public async Task ItemEstoque_SemFornecedor_DeveCriarSemFk()
    {
        // CA11: fornecedor é FK opcional — ausência não rejeita, só cria sem vínculo
        var job = CriarJobMigrando();
        var reg = CriarRegistro("item_estoque", new
        {
            codigo = "ITEM-002",
            nome = "Gaze estéril",
            categoria_nome = "Material cirúrgico"
            // sem fornecedor_nome — deve criar sem fornecedor
        });

        var categoriaExistente = CategoriaEstoque.Criar(EstabelecimentoId, "Material cirúrgico", "hsl(218 70% 50%)", "fa-tag");
        typeof(CategoriaEstoque).GetProperty("Id")!.SetValue(categoriaExistente, 7L);

        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOuNulo(default)).ReturnsAsync(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default))
                     .ReturnsAsync(new List<MigracaoRegistro> { reg });
        _itemRepo.Setup(r => r.ObterPorCodigoOuNulo("ITEM-002", EstabelecimentoId))
                 .ReturnsAsync((ItemInventario?)null);
        _categoriaRepo.Setup(r => r.ObterPorNomeOuNulo("Material cirúrgico", EstabelecimentoId))
                      .ReturnsAsync(categoriaExistente);
        _itemRepo.Setup(r => r.Salvar(It.IsAny<ItemInventario>())).Returns(Task.CompletedTask);
        _registroRepo.Setup(r => r.Salvar(It.IsAny<MigracaoRegistro>(), default)).Returns(Task.CompletedTask);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert
        Assert.That(reg.Status, Is.EqualTo("importado_criado"),
            "Item sem fornecedor (FK opcional) deve ser criado normalmente.");
        Assert.That(job.Status, Is.EqualTo("concluido"));
        // Não deve ter tentado buscar fornecedor (campo ausente no payload)
        _fornecedorRepo.Verify(r => r.ObterPorNomeOuNulo(It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    // ─── CA12 — ordem de FK: fornecedor processado antes de item_estoque ─────────

    [Test]
    public async Task OrdemFk_FornecedorProcessadoAntesDeItemEstoque()
    {
        // CA12: fornecedor_estoque deve aparecer antes de item_estoque na ordem de processamento
        // Isso é verificado pela posição no array OrdemEntidades (não há mocks de execução cruzada
        // porque o handler não itera registros de outro tipo dentro de um tipo, mas validamos a
        // invariante de sequência via lotes separados processados em batch).
        // Aqui verificamos que ambos são suportados e que o fornecedor é criado antes.
        var job = CriarJobMigrando();

        var regFornecedor = CriarRegistro("fornecedor_estoque", new { razao_social = "Dist. Médica BR" });
        var categoriaExistente = CategoriaEstoque.Criar(EstabelecimentoId, "Equipamentos", "hsl(218 70% 50%)", "fa-tag");
        typeof(CategoriaEstoque).GetProperty("Id")!.SetValue(categoriaExistente, 8L);
        var regItem = CriarRegistro("item_estoque", new
        {
            codigo = "EQ-001",
            nome = "Maca hospitalar",
            categoria_nome = "Equipamentos"
        });

        var callOrder = new List<string>();

        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOuNulo(default)).ReturnsAsync(job);
        // Retorna item antes de fornecedor (desordenado) — handler deve reordenar pela FK
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default))
                     .ReturnsAsync(new List<MigracaoRegistro> { regItem, regFornecedor });

        _fornecedorRepo.Setup(r => r.ObterPorCnpjOuNulo(It.IsAny<string>(), EstabelecimentoId))
                       .ReturnsAsync((FornecedorEstoque?)null);
        _fornecedorRepo.Setup(r => r.ObterPorNomeOuNulo("Dist. Médica BR", EstabelecimentoId))
                       .Callback(() => callOrder.Add("fornecedor_lookup"))
                       .ReturnsAsync((FornecedorEstoque?)null);
        _fornecedorRepo.Setup(r => r.Salvar(It.IsAny<FornecedorEstoque>()))
                       .Callback(() => callOrder.Add("fornecedor_salvar"))
                       .Returns(Task.CompletedTask);

        _categoriaRepo.Setup(r => r.ObterPorNomeOuNulo("Equipamentos", EstabelecimentoId))
                      .ReturnsAsync(categoriaExistente);
        _itemRepo.Setup(r => r.ObterPorCodigoOuNulo("EQ-001", EstabelecimentoId))
                 .Callback(() => callOrder.Add("item_lookup"))
                 .ReturnsAsync((ItemInventario?)null);
        _itemRepo.Setup(r => r.Salvar(It.IsAny<ItemInventario>()))
                 .Callback(() => callOrder.Add("item_salvar"))
                 .Returns(Task.CompletedTask);

        _registroRepo.Setup(r => r.Salvar(It.IsAny<MigracaoRegistro>(), default)).Returns(Task.CompletedTask);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert — fornecedor deve ter sido processado ANTES do item (ordem de FK respeitada)
        Assert.That(callOrder.IndexOf("fornecedor_lookup"), Is.LessThan(callOrder.IndexOf("item_lookup")),
            "CA12: fornecedor_estoque deve ser processado antes de item_estoque (respeita ordem de FK).");
        Assert.That(regFornecedor.Status, Is.EqualTo("importado_criado"));
        Assert.That(regItem.Status, Is.EqualTo("importado_criado"));
        Assert.That(job.Status, Is.EqualTo("concluido"));
    }

    // ─── Regressão: dedup de paciente normaliza telefone (Job #15 — não duplicar) ──
    // Bug confirmado: telefone gravado normalizado ("31999999999") mas o dedup buscava
    // com o telefone bruto do payload ("(31) 99999-9999") → nunca achava o existente →
    // criava DUPLICATA a cada reprocessamento. O fix normaliza o valor de busca.

    [Test]
    public async Task ExecutarAsync_PacienteSemCpf_DedupNormalizaTelefone_AtualizaNaoDuplica()
    {
        // Arrange
        var job = CriarJobMigrando();
        var reg = CriarRegistro("paciente", new
        {
            nome = "José Antunes",
            telefone = "(31) 99999-9999",   // formatado no payload
            sexo = "M",
            data_nascimento = "1980-01-01",
        });
        var existente = Paciente.Cadastrar(EstabelecimentoId, "José Antunes", null, null,
            GeneroPaciente.NaoInformado, "31999999999", "", "", "");

        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOuNulo(default)).ReturnsAsync(job);
        _registroRepo.Setup(r => r.ListarPorJob(job.Id, default))
                     .ReturnsAsync(new List<MigracaoRegistro> { reg });
        _registroRepo.Setup(r => r.Salvar(It.IsAny<MigracaoRegistro>(), default)).Returns(Task.CompletedTask);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), default)).Returns(Task.CompletedTask);
        _pacienteRepo.Setup(r => r.Salvar(It.IsAny<Paciente>())).Returns(Task.CompletedTask);
        // O dedup só encontra o existente quando buscado com o telefone NORMALIZADO (só dígitos).
        _pacienteRepo.Setup(r => r.ObterPorNomeTelefoneOuNulo("José Antunes", "31999999999", EstabelecimentoId))
                     .ReturnsAsync(existente);

        // Act
        await _sut.ExecutarAsync(default);

        // Assert: buscou com telefone normalizado, achou e ATUALIZOU (não duplicou).
        _pacienteRepo.Verify(r => r.ObterPorNomeTelefoneOuNulo("José Antunes", "31999999999", EstabelecimentoId), Times.Once);
        _pacienteRepo.Verify(r => r.ObterPorNomeTelefoneOuNulo(It.IsAny<string>(), "(31) 99999-9999", It.IsAny<long>()), Times.Never);
        Assert.That(reg.Status, Is.EqualTo("importado_atualizado"));
    }
}
