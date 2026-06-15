using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Admin.Migracao;

/// <summary>
/// Testes unitários do DesfazerMigracaoCommandHandler (CA17, R9, D12).
///
/// CA principal (CA17): dado um job concluído com 800 criados e 200 atualizados,
/// quando o operador clica "Desfazer", os 800 são revertidos, os 200 mantidos,
/// e o relatório avisa "200 registros atualizados mantidos (não revertidos)".
/// </summary>
[TestFixture]
public class DesfazerMigracaoCommandHandlerTests
{
    private Mock<IMigracaoJobRepository>         _jobRepo;
    private Mock<IMigracaoRegistroRepository>    _registroRepo;
    private Mock<IPacienteRepository>            _pacienteRepo;
    private Mock<IAgendamentoRepository>         _agendamentoRepo;
    private Mock<IItemInventarioRepository>      _itemRepo;
    private Mock<ICategoriaEstoqueRepository>    _categoriaRepo;
    private Mock<IFabricanteEstoqueRepository>   _fabricanteRepo;
    private Mock<IFornecedorEstoqueRepository>   _fornecedorRepo;
    private Mock<ILocalEstoqueRepository>        _localRepo;
    private Mock<ICatalogoCirurgiaRepository>    _cirurgiaRepo;
    private Mock<ICatalogoProdutoRepository>     _produtoRepo;

    private DesfazerMigracaoCommandHandler _sut;

    private const long JobId              = 77L;
    private const long EstabelecimentoId  = 42L;

    [SetUp]
    public void SetUp()
    {
        _jobRepo        = new Mock<IMigracaoJobRepository>();
        _registroRepo   = new Mock<IMigracaoRegistroRepository>();
        _pacienteRepo   = new Mock<IPacienteRepository>();
        _agendamentoRepo = new Mock<IAgendamentoRepository>();
        _itemRepo       = new Mock<IItemInventarioRepository>();
        _categoriaRepo  = new Mock<ICategoriaEstoqueRepository>();
        _fabricanteRepo = new Mock<IFabricanteEstoqueRepository>();
        _fornecedorRepo = new Mock<IFornecedorEstoqueRepository>();
        _localRepo      = new Mock<ILocalEstoqueRepository>();
        _cirurgiaRepo   = new Mock<ICatalogoCirurgiaRepository>();
        _produtoRepo    = new Mock<ICatalogoProdutoRepository>();

        _sut = new DesfazerMigracaoCommandHandler(
            _jobRepo.Object,
            _registroRepo.Object,
            _pacienteRepo.Object,
            _agendamentoRepo.Object,
            _itemRepo.Object,
            _categoriaRepo.Object,
            _fabricanteRepo.Object,
            _fornecedorRepo.Object,
            _localRepo.Object,
            _cirurgiaRepo.Object,
            _produtoRepo.Object,
            NullLogger<DesfazerMigracaoCommandHandler>.Instance);
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private MigracaoJob CriarJobConcluido(string status = MigracaoJob.StatusConcluido)
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, Guid.NewGuid());
        // Avança o job para o status desejado via reflexão (não há ctor público com estado interno)
        // Em vez disso, criamos via as transições legítimas.
        // Addendum 003: upload → aguardando_aprovacao; AprovarAnalise → aguardando_mapa.
        job.RegistrarArquivoRecebido("s3://key");
        job.AprovarAnalise(Guid.NewGuid());
        job.MarcarMapaEmRevisao();
        job.MarcarPreviewPronto(Guid.NewGuid());
        job.MarcarMigrando(Guid.NewGuid());
        if (status == MigracaoJob.StatusConcluido)
            job.MarcarConcluido();
        else
            job.MarcarConcluidoComErros();
        return job;
    }

    private static MigracaoRegistro CriarRegistroCriado(long entidadeAlvoId, string entidade = "paciente")
    {
        var reg = MigracaoRegistro.Criar(JobId, EstabelecimentoId, entidade, "{}");
        reg.MarcarImportadoCriado(entidadeAlvoId);
        return reg;
    }

    private static MigracaoRegistro CriarRegistroAtualizado(long entidadeAlvoId, string entidade = "paciente")
    {
        var reg = MigracaoRegistro.Criar(JobId, EstabelecimentoId, entidade, "{}");
        reg.MarcarImportadoAtualizado(entidadeAlvoId);
        return reg;
    }

    // ─── CA17: 800 criados revertidos, 200 atualizados mantidos ──────────────

    [Test]
    public async Task Handle_CriadosRevertidos_AtualizadosMantidos_RelatorioCerto()
    {
        // Arrange
        var job = CriarJobConcluido();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

        // 800 criados + 200 atualizados
        var criados      = Enumerable.Range(1, 800).Select(i => CriarRegistroCriado(i)).ToList();
        var atualizados  = Enumerable.Range(801, 200).Select(i => CriarRegistroAtualizado(i)).ToList();
        var todos        = criados.Concat(atualizados).ToList();

        _registroRepo.Setup(r => r.ListarPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(todos);
        _registroRepo.Setup(r => r.ListarCriadosPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(criados);

        // Mock: retorna paciente qualquer para cada ID
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(It.IsAny<long>(), EstabelecimentoId))
                     .ReturnsAsync((long id, long _) => CriarPacienteFake(id));
        _pacienteRepo.Setup(r => r.Remover(It.IsAny<Paciente>()))
                     .Returns(Task.CompletedTask);

        // Act
        var resultado = await _sut.Handle(JobId);

        // Assert — CA17
        Assert.That(resultado.TotalRevertidos,          Is.EqualTo(800), "800 criados devem ser revertidos");
        Assert.That(resultado.TotalNaoRevertidos,       Is.EqualTo(0),   "sem falhas de FK esperadas");
        Assert.That(resultado.TotalAtualizadosMantidos, Is.EqualTo(200), "200 atualizados devem ser mantidos");
        Assert.That(resultado.Aviso, Does.Contain("200"),                "aviso deve mencionar os 200 atualizados mantidos");
        Assert.That(resultado.Aviso, Does.Contain("não revertidos"),     "aviso deve deixar claro que atualizados não foram revertidos");

        // Job deve ter mudado para desfeito
        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusDesfeito));

        // Nenhum Remover chamado nos atualizados (inexistentes na listagem de criados)
        _pacienteRepo.Verify(r => r.Remover(It.IsAny<Paciente>()), Times.Exactly(800));
    }

    // ─── CA17: apenas criados (sem atualizados) ──────────────────────────────

    [Test]
    public async Task Handle_SomenteCriados_AvisosemAtualizados()
    {
        var job = CriarJobConcluido();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

        var criados = new List<MigracaoRegistro> { CriarRegistroCriado(1) };
        _registroRepo.Setup(r => r.ListarPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(criados);
        _registroRepo.Setup(r => r.ListarCriadosPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(criados);

        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(1L, EstabelecimentoId))
                     .ReturnsAsync(CriarPacienteFake(1));
        _pacienteRepo.Setup(r => r.Remover(It.IsAny<Paciente>())).Returns(Task.CompletedTask);

        var resultado = await _sut.Handle(JobId);

        Assert.That(resultado.TotalRevertidos,          Is.EqualTo(1));
        Assert.That(resultado.TotalAtualizadosMantidos, Is.EqualTo(0));
        // Quando não há atualizados, aviso não menciona "atualizados"
        Assert.That(resultado.Aviso, Does.Not.Contain("atualizados mantidos"));
    }

    // ─── R9: job não concluído não pode ser desfeito ─────────────────────────

    [Test]
    public void Handle_JobNaoConcluido_LancaBusinessException()
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, Guid.NewGuid());
        // Status = aguardando_arquivo — não é concluído
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
        _registroRepo.Setup(r => r.ListarPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync([]);
        _registroRepo.Setup(r => r.ListarCriadosPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync([]);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(JobId));
    }

    // ─── Job inexistente → BusinessException ─────────────────────────────────

    [Test]
    public void Handle_JobNaoEncontrado_LancaBusinessException()
    {
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((MigracaoJob?)null);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(JobId));
    }

    // ─── Registro criado referenciado por FK → não reverte, reporta ──────────

    [Test]
    public async Task Handle_FkAtiva_RegistroNaoRevertido_ReportadoNoRelatorio()
    {
        var job = CriarJobConcluido();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

        var criado = CriarRegistroCriado(10, "paciente");
        _registroRepo.Setup(r => r.ListarPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync([criado]);
        _registroRepo.Setup(r => r.ListarCriadosPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync([criado]);

        // FK ativa: Remover lança exceção de constraint
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(10L, EstabelecimentoId))
                     .ReturnsAsync(CriarPacienteFake(10));
        _pacienteRepo.Setup(r => r.Remover(It.IsAny<Paciente>()))
                     .ThrowsAsync(new Exception("FK constraint violated"));

        var resultado = await _sut.Handle(JobId);

        Assert.That(resultado.TotalRevertidos,    Is.EqualTo(0));
        Assert.That(resultado.TotalNaoRevertidos, Is.EqualTo(1), "Registro com FK deve aparecer como não-revertido");
        Assert.That(resultado.Aviso,              Does.Contain("referenciados por outro fluxo"));
    }

    // ─── Entidade não encontrada (já removida por outro path) ────────────────

    [Test]
    public async Task Handle_EntidadeNaoEncontrada_NaoRevertidoSemErro()
    {
        var job = CriarJobConcluido();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

        var criado = CriarRegistroCriado(99, "paciente");
        _registroRepo.Setup(r => r.ListarPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync([criado]);
        _registroRepo.Setup(r => r.ListarCriadosPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync([criado]);

        // Entidade não encontrada — retorna null
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(99L, EstabelecimentoId))
                     .ReturnsAsync((Paciente?)null);

        var resultado = await _sut.Handle(JobId);

        Assert.That(resultado.TotalRevertidos,    Is.EqualTo(0));
        Assert.That(resultado.TotalNaoRevertidos, Is.EqualTo(1));
        _pacienteRepo.Verify(r => r.Remover(It.IsAny<Paciente>()), Times.Never);
    }

    // ─── Múltiplos tipos de entidade são revertidos na ordem correta ─────────

    [Test]
    public async Task Handle_MultiplaEntidades_OrderReversa_TodasRevertidas()
    {
        var job = CriarJobConcluido();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

        var paciente    = CriarRegistroCriado(1, "paciente");
        var item        = CriarRegistroCriado(2, "item_estoque");
        var categoria   = CriarRegistroCriado(3, "categoria_estoque");
        var agendamento = CriarRegistroCriado(4, "agendamento");

        var criados = new List<MigracaoRegistro> { paciente, item, categoria, agendamento };
        _registroRepo.Setup(r => r.ListarPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(criados);
        _registroRepo.Setup(r => r.ListarCriadosPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(criados);

        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(1L, EstabelecimentoId))
                     .ReturnsAsync(CriarPacienteFake(1));
        _pacienteRepo.Setup(r => r.Remover(It.IsAny<Paciente>())).Returns(Task.CompletedTask);

        _itemRepo.Setup(r => r.ObterPorIdOuNulo(2L, EstabelecimentoId))
                 .ReturnsAsync(CriarItemFake(2, EstabelecimentoId));
        _itemRepo.Setup(r => r.Remover(It.IsAny<ItemInventario>())).Returns(Task.CompletedTask);

        _categoriaRepo.Setup(r => r.ObterPorIdOuNulo(3L, EstabelecimentoId))
                      .ReturnsAsync(CriarCategoriaFake(3));
        _categoriaRepo.Setup(r => r.Remover(It.IsAny<CategoriaEstoque>())).Returns(Task.CompletedTask);

        _agendamentoRepo.Setup(r => r.ObterPorIdOuNulo(4L, EstabelecimentoId))
                        .ReturnsAsync(CriarAgendamentoFake(4));
        _agendamentoRepo.Setup(r => r.Remover(It.IsAny<Agendamento>())).Returns(Task.CompletedTask);

        var resultado = await _sut.Handle(JobId);

        Assert.That(resultado.TotalRevertidos, Is.EqualTo(4));
        Assert.That(resultado.TotalNaoRevertidos, Is.EqualTo(0));
    }

    // ─── Job concluído com erros também pode ser desfeito ────────────────────

    [Test]
    public async Task Handle_JobConcluidoComErros_PodeSerDesfeito()
    {
        var job = CriarJobConcluido(MigracaoJob.StatusConcluidoComErros);
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
        _registroRepo.Setup(r => r.ListarPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync([]);
        _registroRepo.Setup(r => r.ListarCriadosPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync([]);

        var resultado = await _sut.Handle(JobId);

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusDesfeito));
        Assert.That(resultado.TotalRevertidos, Is.EqualTo(0));
    }

    // ─── LGPD: sem PII em log (verificação por ausência de Setup capturando dados) ──

    [Test]
    public async Task Handle_Audit_StatusJobAlteradoParaDesfeito()
    {
        var job = CriarJobConcluido();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
        _registroRepo.Setup(r => r.ListarPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync([]);
        _registroRepo.Setup(r => r.ListarCriadosPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync([]);

        await _sut.Handle(JobId);

        // Audit: Job salvo com status desfeito (CA20)
        _jobRepo.Verify(r => r.Salvar(It.Is<MigracaoJob>(j => j.Status == MigracaoJob.StatusDesfeito), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Factories fake ─────────────────────────────────────────────────────

    private static Paciente CriarPacienteFake(long id)
    {
        var p = Paciente.Cadastrar(EstabelecimentoId, "Paciente Teste", null, null, GeneroPaciente.NaoInformado, null, null, null, null, null);
        // Setar ID via reflection para simular entidade persistida
        typeof(Paciente).BaseType!.GetProperty("Id")!.SetValue(p, id);
        return p;
    }

    private static ItemInventario CriarItemFake(long id, long estId)
    {
        var item = ItemInventario.Criar(estId, $"COD{id}", $"Item {id}", 1, "Categoria", "un", 0, null, null, null, null);
        typeof(ItemInventario).BaseType!.GetProperty("Id")!.SetValue(item, id);
        return item;
    }

    private static CategoriaEstoque CriarCategoriaFake(long id)
    {
        var cat = CategoriaEstoque.Criar(EstabelecimentoId, $"Cat {id}", "hsl(218 70% 50%)", "fa-box");
        typeof(CategoriaEstoque).BaseType!.GetProperty("Id")!.SetValue(cat, id);
        return cat;
    }

    private static Agendamento CriarAgendamentoFake(long id)
    {
        var ag = Agendamento.Criar(EstabelecimentoId, 1, Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1), "Consulta", null);
        typeof(Agendamento).BaseType!.GetProperty("Id")!.SetValue(ag, id);
        return ag;
    }
}
