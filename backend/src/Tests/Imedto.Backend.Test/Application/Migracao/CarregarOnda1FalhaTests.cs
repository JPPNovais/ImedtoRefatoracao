using Imedto.Backend.Application.Migracao.Jobs;
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

namespace Imedto.Backend.Test.Application.Migracao;

/// <summary>
/// CA26 — addendum 002: CarregarOnda1JobHandler marca falhou ao invés de re-lançar.
/// CA27 — CA27 é testado em CarregarOnda2FalhaTests (espera legítima não vira falha).
/// </summary>
[TestFixture]
public class CarregarOnda1FalhaTests
{
    private Mock<IMigracaoJobRepository> _jobRepo;
    private Mock<IMigracaoRegistroRepository> _registroRepo;
    private Mock<IMigracaoJobEventoRepository> _eventoRepo;
    private Mock<IPacienteRepository> _pacienteRepo;
    private Mock<ICategoriaEstoqueRepository> _categoriaRepo;
    private Mock<IFabricanteEstoqueRepository> _fabricanteRepo;
    private Mock<IFornecedorEstoqueRepository> _fornecedorRepo;
    private Mock<ILocalEstoqueRepository> _localRepo;
    private Mock<IItemInventarioRepository> _itemRepo;
    private Mock<IAgendamentoRepository> _agendamentoRepo;
    private Mock<ICatalogoCirurgiaRepository> _cirurgiaRepo;
    private Mock<ICatalogoProdutoRepository> _produtoRepo;
    private Mock<IMigracaoCatalogoCirurgiaLookup> _cirurgiaLookup;
    private Mock<IMigracaoCatalogoProdutoLookup> _produtoLookup;
    private Mock<IMigracaoPacienteLookup>        _pacienteLookup;
    private Mock<IMigracaoAgendamentoLookup>     _agendamentoLookup;

    private static readonly Guid AdminId = Guid.NewGuid();
    private const long EstId = 42L;
    private const long JobId = 55L;

    [SetUp]
    public void SetUp()
    {
        _jobRepo       = new Mock<IMigracaoJobRepository>();
        _registroRepo  = new Mock<IMigracaoRegistroRepository>();
        _eventoRepo    = new Mock<IMigracaoJobEventoRepository>();
        _eventoRepo.Setup(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);
        _pacienteRepo  = new Mock<IPacienteRepository>();
        _categoriaRepo = new Mock<ICategoriaEstoqueRepository>();
        _fabricanteRepo = new Mock<IFabricanteEstoqueRepository>();
        _fornecedorRepo = new Mock<IFornecedorEstoqueRepository>();
        _localRepo     = new Mock<ILocalEstoqueRepository>();
        _itemRepo      = new Mock<IItemInventarioRepository>();
        _agendamentoRepo = new Mock<IAgendamentoRepository>();
        _cirurgiaRepo  = new Mock<ICatalogoCirurgiaRepository>();
        _produtoRepo   = new Mock<ICatalogoProdutoRepository>();
        _cirurgiaLookup    = new Mock<IMigracaoCatalogoCirurgiaLookup>();
        _produtoLookup     = new Mock<IMigracaoCatalogoProdutoLookup>();
        _pacienteLookup    = new Mock<IMigracaoPacienteLookup>();
        _agendamentoLookup = new Mock<IMigracaoAgendamentoLookup>();
    }

    private CarregarOnda1JobHandler CriarSut() => new(
        _jobRepo.Object, _registroRepo.Object, _eventoRepo.Object,
        _pacienteRepo.Object,
        _categoriaRepo.Object, _fabricanteRepo.Object, _fornecedorRepo.Object,
        _localRepo.Object, _itemRepo.Object, _agendamentoRepo.Object,
        _cirurgiaRepo.Object, _produtoRepo.Object,
        _cirurgiaLookup.Object, _produtoLookup.Object,
        _pacienteLookup.Object, _agendamentoLookup.Object,
        NullLogger<CarregarOnda1JobHandler>.Instance);

    private MigracaoJob CriarJobMigrando()
    {
        var job = MigracaoJob.Criar(EstId, AdminId);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        // Addendum 003: upload → aguardando_aprovacao; AprovarAnalise → aguardando_mapa.
        job.RegistrarArquivoRecebido("migracao/42/55/arquivo.zip");
        job.AprovarAnalise(Guid.NewGuid());
        job.MarcarMapaEmRevisao();
        job.MarcarPreviewPronto(AdminId);
        job.MarcarMigrando(AdminId);
        return job;
    }

    /// <summary>
    /// CA26 — quando ListarPorJob lança exceção inesperada,
    /// o job deve ser marcado como falhou em vez de re-lançar a exceção.
    /// </summary>
    [Test]
    public async Task ExecutarAsync_ExcecaoInesperada_MarcaJobFalhou()
    {
        var job = CriarJobMigrando();
        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOuNulo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        // Simula falha inesperada no banco ao listar registros.
        _registroRepo.Setup(r => r.ListarPorJob(JobId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("timeout de conexão"));

        MigracaoJob? jobSalvo = null;
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Callback<MigracaoJob, CancellationToken>((j, _) => jobSalvo = j)
            .Returns(Task.CompletedTask);

        var sut = CriarSut();

        // Não deve propagar a exceção (antes do addendum, propagava e travava o job).
        Assert.DoesNotThrowAsync(() => sut.ExecutarAsync(CancellationToken.None));

        Assert.That(jobSalvo, Is.Not.Null, "Job deve ser salvo como falhou.");
        Assert.That(jobSalvo!.Status, Is.EqualTo(MigracaoJob.StatusFalhou));
        Assert.That(jobSalvo.MotivoFalha, Is.EqualTo("falha inesperada na carga"));
        Assert.That(jobSalvo.StatusAntesFalha, Is.EqualTo(MigracaoJob.StatusMigrando));
    }
}
