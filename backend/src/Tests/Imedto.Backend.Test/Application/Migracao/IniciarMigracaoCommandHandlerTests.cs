using Imedto.Backend.Application.Migracao.Commands;
using Imedto.Backend.Contracts.Migracao.Commands;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Migracao;

/// <summary>
/// Testes do handler IniciarMigracaoCommandHandler (briefing 2026-06-15_001 — Marco 1).
/// Cobrem: CA19 (limite 50MB), CA2/CA3 (multi-tenant), CA4 (PII em logs/mensagens), CA20 (estados),
///         CA53 (evento forward-only persistido na transição — addendum 004).
/// </summary>
[TestFixture]
public class IniciarMigracaoCommandHandlerTests
{
    private Mock<IMigracaoJobRepository> _repo;
    private Mock<IMigracaoArquivoStorageService> _storage;
    private Mock<IMigracaoJobEventoRepository> _eventoRepo;
    private IniciarMigracaoCommandHandler _sut;

    private const long EstabelecimentoId = 10;
    private const long LimiteBytes = 50L * 1024 * 1024; // 50 MB
    private static readonly Guid UsuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo       = new Mock<IMigracaoJobRepository>();
        _storage    = new Mock<IMigracaoArquivoStorageService>();
        _eventoRepo = new Mock<IMigracaoJobEventoRepository>();

        // Salvar atribui Id simulado na primeira chamada (para a S3 key conter um Id válido).
        var chamadas = 0;
        _repo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
             .Callback<MigracaoJob, CancellationToken>((job, _) =>
             {
                 if (chamadas++ == 0)
                     // Simula EF gerando Id após INSERT — padrão dos outros handlers de teste.
                     typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, 99L);
             })
             .Returns(Task.CompletedTask);

        _storage.Setup(s => s.UploadArquivoAsync(
                    It.IsAny<long>(), It.IsAny<long>(),
                    It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("migracao/10/99/arquivo.zip");

        _eventoRepo.Setup(e => e.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        _sut = new IniciarMigracaoCommandHandler(_repo.Object, _storage.Object, _eventoRepo.Object);
    }

    private static IniciarMigracaoCommand CmdValido(long tamanhoBytes = 1024) =>
        new()
        {
            EstabelecimentoId   = EstabelecimentoId,
            UsuarioId           = UsuarioId,
            ArquivoStream       = Stream.Null,
            ArquivoTamanhoBytes = tamanhoBytes,
        };

    // ─── CA19 — Limite de 50MB ──────────────────────────────────────────────────

    /// <summary>CA19 — arquivo exatamente 50MB deve ser aceito (limite inclusivo).</summary>
    [Test]
    public async Task Handle_Arquivo50MB_Aceito()
    {
        var cmd = CmdValido(tamanhoBytes: LimiteBytes);

        var result = await _sut.Handle(cmd);

        // Addendum 003 — R-A1: após upload, job vai para aguardando_aprovacao.
        Assert.That(result.Status, Is.EqualTo(MigracaoJob.StatusAguardandoAprovacao));
    }

    /// <summary>CA19 — arquivo 1 byte acima de 50MB deve ser rejeitado antes de qualquer I/O.</summary>
    [Test]
    public void Handle_Arquivo50MB_Mais1Byte_LancaBusinessException()
    {
        var cmd = CmdValido(tamanhoBytes: LimiteBytes + 1);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));

        // Mensagem de negócio amigável, sem dados técnicos internos.
        Assert.That(ex!.Message, Does.Contain("50MB"));
        // Nenhuma I/O deve ter ocorrido.
        _repo.Verify(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()), Times.Never);
        _storage.Verify(s => s.UploadArquivoAsync(
            It.IsAny<long>(), It.IsAny<long>(),
            It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Handle_ArquivoTamanhoZero_LancaBusinessException()
    {
        var cmd = CmdValido(tamanhoBytes: 0);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
    }

    // ─── Fluxo feliz ────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_Valido_CriaJobEFazUploadES3()
    {
        var cmd = CmdValido();

        var result = await _sut.Handle(cmd);

        Assert.That(result.JobId, Is.EqualTo(99L));
        // Addendum 003 — R-A1: após upload, job vai para aguardando_aprovacao.
        Assert.That(result.Status, Is.EqualTo(MigracaoJob.StatusAguardandoAprovacao));

        _storage.Verify(s => s.UploadArquivoAsync(
            EstabelecimentoId, 99L, Stream.Null, It.IsAny<CancellationToken>()), Times.Once);

        // Salvar chamado 2×: após criar job (para Id) e após RegistrarArquivoRecebido.
        _repo.Verify(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // ─── CA53 — Evento forward-only na transição (addendum 004) ─────────────────

    /// <summary>
    /// CA53 — Após upload bem-sucedido, um evento deve ser gravado com
    /// StatusAnterior = "aguardando_arquivo" e StatusNovo = "aguardando_aprovacao".
    /// UsuarioId é null (upload é ação do tenant, sem admin autenticado aqui).
    /// </summary>
    [Test]
    public async Task Handle_Valido_GravaEventoDeTransicao()
    {
        var cmd = CmdValido();

        await _sut.Handle(cmd);

        _eventoRepo.Verify(
            e => e.Gravar(
                It.Is<MigracaoJobEvento>(ev =>
                    ev.StatusAnterior == MigracaoJob.StatusAguardandoArquivo
                    && ev.StatusNovo  == MigracaoJob.StatusAguardandoAprovacao
                    && ev.UsuarioId   == null),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Deve gravar exatamente 1 evento de transição aguardando_arquivo→aguardando_aprovacao.");
    }

    /// <summary>CA53 — quando upload falha, nenhum evento de transição deve ser gravado.</summary>
    [Test]
    public void Handle_FalhaS3_NaoGravaEvento()
    {
        _storage.Setup(s => s.UploadArquivoAsync(
                    It.IsAny<long>(), It.IsAny<long>(),
                    It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("S3 indisponível"));

        var cmd = CmdValido();

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));

        // Falha no upload → sem evento de transição (o job fica como rejeitado internamente).
        _eventoRepo.Verify(
            e => e.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── Falha no S3 — CA20 + fail-safe ─────────────────────────────────────────

    [Test]
    public void Handle_FalhaS3_RejetaJobELancaBusinessException()
    {
        _storage.Setup(s => s.UploadArquivoAsync(
                    It.IsAny<long>(), It.IsAny<long>(),
                    It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("S3 indisponível"));

        MigracaoJob? jobCapturado = null;
        _repo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>()))
             .Callback<MigracaoJob, CancellationToken>((job, _) => jobCapturado = job)
             .Returns(Task.CompletedTask);

        var cmd = CmdValido();

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));

        // Mensagem genérica — sem detalhes técnicos (CA4).
        Assert.That(ex!.Message, Does.Contain("Falha ao processar"));
        Assert.That(ex!.Message, Does.Not.Contain("S3"));
        Assert.That(ex!.Message, Does.Not.Contain("indisponível"));

        // Job foi rejeitado (não fica preso em aguardando_arquivo).
        Assert.That(jobCapturado?.Status, Is.EqualTo(MigracaoJob.StatusRejeitado));
    }

    // ─── CA4 — Sem PII na mensagem de erro ──────────────────────────────────────

    [Test]
    public void Handle_Arquivo50MB_Mais1_MensagemNaoContemPII()
    {
        var cmd = CmdValido(tamanhoBytes: LimiteBytes + 1);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));

        // A mensagem não deve mencionar o nome do arquivo, ids de usuário etc.
        Assert.That(ex!.Message, Does.Not.Contain(EstabelecimentoId.ToString()));
        Assert.That(ex!.Message, Does.Not.Contain(UsuarioId.ToString()));
    }
}
