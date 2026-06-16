using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Admin.Migracao;

/// <summary>
/// Testes do AprovarAnaliseCommandHandler (addendum 003 — CA41/CA42/CA46).
/// </summary>
[TestFixture]
public class AprovarAnaliseCommandHandlerTests
{
    private Mock<IMigracaoJobRepository>       _jobRepo;
    private Mock<IMigracaoJobEventoRepository> _eventoRepo;
    private AprovarAnaliseCommandHandler       _sut;

    private const long EstabelecimentoId = 42;
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid AdminId   = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _jobRepo    = new Mock<IMigracaoJobRepository>();
        _eventoRepo = new Mock<IMigracaoJobEventoRepository>();
        _eventoRepo.Setup(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);
        _sut = new AprovarAnaliseCommandHandler(_jobRepo.Object, _eventoRepo.Object);
    }

    private static MigracaoJob CriarJobEmAguardandoAprovacao(long id = 1L)
    {
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        // Simula ID persistido para que MigracaoJobEvento.Criar não rejeite Id <= 0
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, id);
        job.RegistrarArquivoRecebido("migracao/42/1/arquivo.zip");
        // Pós upload → aguardando_aprovacao (addendum 003 R-A1).
        return job;
    }

    // ─── CA41 — aprovar job válido ───────────────────────────────────────────────

    [Test]
    public async Task Handle_JobEmAguardandoAprovacao_TransicionaParaAguardandoMapa()
    {
        var job = CriarJobEmAguardandoAprovacao();
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.Handle(new AprovarAnaliseCommand { JobId = 1, AdminId = AdminId });

        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusAguardandoMapa));
        _jobRepo.Verify(r => r.Salvar(job, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── CA42 — aprovar fora do estado certo → 422 ──────────────────────────────

    [Test]
    public async Task Handle_JobNaoEmAguardandoAprovacao_LancaBusinessException()
    {
        // Job em aguardando_arquivo (não teve upload ainda).
        var job = MigracaoJob.Criar(EstabelecimentoId, UsuarioId);
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        Assert.ThrowsAsync<BusinessException>(() =>
            _sut.Handle(new AprovarAnaliseCommand { JobId = 1, AdminId = AdminId }));

        _jobRepo.Verify(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── CA46 — job não encontrado → mensagem genérica (multi-tenant seguro) ────

    [Test]
    public async Task Handle_JobNaoEncontrado_LancaBusinessExceptionGenerica()
    {
        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MigracaoJob?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.Handle(new AprovarAnaliseCommand { JobId = 999, AdminId = AdminId }));

        // CA46 — mensagem genérica: não vaza existência de outro tenant.
        Assert.That(ex!.Message, Is.EqualTo("Não encontrado."));
    }

    [Test]
    public async Task Handle_JobIdInvalido_LancaBusinessException()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.Handle(new AprovarAnaliseCommand { JobId = 0, AdminId = AdminId }));

        Assert.That(ex!.Message, Does.Contain("inválido"));
    }

    [Test]
    public async Task Handle_AdminIdVazio_LancaBusinessException()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.Handle(new AprovarAnaliseCommand { JobId = 1, AdminId = Guid.Empty }));

        Assert.That(ex!.Message, Does.Contain("Admin"));
    }
}
