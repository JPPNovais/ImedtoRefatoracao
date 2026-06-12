using Imedto.Backend.Application.Termos.Commands;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Termos;

/// <summary>
/// Testes unitários do handler de anexo de PDF — briefing 2026-06-12_002, bloco B.
/// Cobre: CA-B1 a CA-B8.
/// </summary>
[TestFixture]
public class AnexarPdfTermoCommandHandlerTests
{
    private Mock<ITermoEmitidoRepository> _repo = null!;
    private Mock<ITermoPdfStorageService> _storage = null!;
    private Mock<ITermoImagemParaPdfConverter> _converter = null!;
    private Mock<ITermoAuditLogger> _audit = null!;
    private Mock<IEventBus> _eventBus = null!;
    private AnexarPdfTermoCommandHandler _sut = null!;

    private const long EstabId = 1;
    private const long TermoId = 5;
    private readonly Guid _solicitante = Guid.NewGuid();

    // Magic bytes válidos
    private static readonly byte[] PdfMagic  = [0x25, 0x50, 0x44, 0x46, 0x2D]; // %PDF-
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF, 0x00, 0x00, 0x00];
    private static readonly byte[] PngMagic  = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ITermoEmitidoRepository>();
        _storage = new Mock<ITermoPdfStorageService>();
        _converter = new Mock<ITermoImagemParaPdfConverter>();
        _audit = new Mock<ITermoAuditLogger>();
        _eventBus = new Mock<IEventBus>();
        _sut = new AnexarPdfTermoCommandHandler(
            _repo.Object, _storage.Object, _converter.Object, _audit.Object, _eventBus.Object);
    }

    private TermoEmitido TermoPendente()
    {
        var t = TermoEmitido.Emitir(10, EstabId, 99, 1, "<p>x</p>", "x", Guid.NewGuid());
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(t, TermoId);
        return t;
    }

    private AnexarPdfTermoCommand CmdComPartes(params (byte[] Dados, string Mime)[] partes) => new()
    {
        TermoEmitidoId = TermoId,
        EstabelecimentoId = EstabId,
        SolicitanteUsuarioId = _solicitante,
        Partes = partes.Select(p => ((Stream)new MemoryStream(p.Dados), p.Mime, (long)p.Dados.Length)).ToList(),
        TamanhoTotalBytes = partes.Sum(p => p.Dados.Length),
    };

    // ── CA-B1: PDF com magic bytes corretos → sucesso ─────────────────────────

    [Test]
    public async Task Handle_PdfValido_AnexaEAuditaHash()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(TermoPendente());

        var cmd = CmdComPartes((PdfMagic, "application/pdf"));
        await _sut.Handle(cmd);

        Assert.That(cmd.StoragePath, Does.StartWith("termos/est_1/"));
        Assert.That(cmd.PdfHash, Has.Length.EqualTo(64));
        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), "application/pdf", It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.RegistrarAsync(EstabId, _solicitante, "termo-pdf-anexado", "TermoEmitido", TermoId,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── CA-B2: Imagem JPEG válida → converter chamado ──────────────────────────

    [Test]
    public async Task Handle_JpegValido_ChamaConverterEAnexaPdf()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(TermoPendente());
        _converter.Setup(c => c.ConverterParaPdf(It.IsAny<IReadOnlyList<(Stream, string)>>()))
            .Returns(PdfMagic.Concat(new byte[100]).ToArray());

        var cmd = CmdComPartes((JpegMagic, "image/jpeg"));
        await _sut.Handle(cmd);

        _converter.Verify(c => c.ConverterParaPdf(It.IsAny<IReadOnlyList<(Stream, string)>>()), Times.Once);
        Assert.That(cmd.StoragePath, Does.StartWith("termos/est_1/"));
    }

    // ── CA-B3: Imagem PNG válida → converter chamado ────────────────────────────

    [Test]
    public async Task Handle_PngValido_ChamaConverterEAnexaPdf()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(TermoPendente());
        _converter.Setup(c => c.ConverterParaPdf(It.IsAny<IReadOnlyList<(Stream, string)>>()))
            .Returns(PdfMagic.Concat(new byte[100]).ToArray());

        var cmd = CmdComPartes((PngMagic, "image/png"));
        await _sut.Handle(cmd);

        _converter.Verify(c => c.ConverterParaPdf(It.IsAny<IReadOnlyList<(Stream, string)>>()), Times.Once);
    }

    // ── CA-B4: 2 imagens (frente + verso) → converter chamado com 2 partes ─────

    [Test]
    public async Task Handle_DuasImagens_ConverterChamaComDuasPartes()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(TermoPendente());
        IReadOnlyList<(Stream, string)>? capturado = null;
        _converter.Setup(c => c.ConverterParaPdf(It.IsAny<IReadOnlyList<(Stream, string)>>()))
            .Callback<IReadOnlyList<(Stream, string)>>(l => capturado = l)
            .Returns(PdfMagic.Concat(new byte[100]).ToArray());

        var cmd = CmdComPartes((JpegMagic, "image/jpeg"), (JpegMagic, "image/jpeg"));
        await _sut.Handle(cmd);

        Assert.That(capturado, Is.Not.Null);
        Assert.That(capturado!.Count, Is.EqualTo(2));
    }

    // ── CA-B5: HEIC → 422 com mensagem orientativa ──────────────────────────────

    [Test]
    public void Handle_FormatoHeic_LancaMensagemOrientativa()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(TermoPendente());

        var cmd = CmdComPartes(([0x00, 0x00, 0x00], "image/heic"));
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("HEIC").Or.Contain("JPG").Or.Contain("PNG"));
    }

    [Test]
    public void Handle_FormatoHeif_LancaMensagemOrientativa()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(TermoPendente());

        var cmd = CmdComPartes(([0x00, 0x00, 0x00], "image/heif"));
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("HEIC").Or.Contain("JPG").Or.Contain("PNG"));
    }

    // ── CA-B6: Magic bytes inválidos para o MIME declarado → 422 ───────────────

    [Test]
    public void Handle_PdfComMagicBytesInvalidos_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(TermoPendente());

        // MIME diz PDF mas bytes são de imagem
        var cmd = CmdComPartes((JpegMagic, "application/pdf"));
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("PDF").Or.Contain("válido"));
    }

    [Test]
    public void Handle_JpegComMagicBytesInvalidos_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(TermoPendente());

        // MIME diz JPEG mas bytes são de PDF
        var cmd = CmdComPartes((PdfMagic, "image/jpeg"));
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("imagem").Or.Contain("inválid"));
    }

    // ── CA-B7: mais de 2 arquivos → 422 ─────────────────────────────────────────

    [Test]
    public void Handle_TresArquivos_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(TermoPendente());

        var cmd = CmdComPartes(
            (JpegMagic, "image/jpeg"),
            (JpegMagic, "image/jpeg"),
            (JpegMagic, "image/jpeg"));
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("2").Or.Contain("máximo"));
    }

    // ── CA-B8: tamanho total > 10 MB → 422 ──────────────────────────────────────

    [Test]
    public void Handle_TamanhoExcede10MB_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(TermoPendente());

        // TamanhoTotalBytes declarado excede limite (sem de fato alocar 10MB no teste)
        var cmd = new AnexarPdfTermoCommand
        {
            TermoEmitidoId = TermoId,
            EstabelecimentoId = EstabId,
            SolicitanteUsuarioId = _solicitante,
            Partes = [(new MemoryStream(PdfMagic), "application/pdf", 11 * 1024 * 1024)],
            TamanhoTotalBytes = 11 * 1024 * 1024,
        };
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("10").Or.Contain("MB").Or.Contain("excede"));
    }

    // ── Multi-tenant: termo de outro tenant → 422 genérico ──────────────────────

    [Test]
    public void Handle_TermoDeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync((TermoEmitido)null);

        var cmd = CmdComPartes((PdfMagic, "application/pdf"));
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Is.EqualTo("Termo não encontrado."));
    }

    // ── PDF duplicado (já tem PdfUrl) → 422 ─────────────────────────────────────

    [Test]
    public void Handle_TermoJaAssinado_LancaBusinessException()
    {
        var t = TermoPendente();
        t.AnexarPdf("path/antigo.pdf", new string('a', 64));  // já assinado
        _repo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(t);

        var cmd = CmdComPartes((PdfMagic, "application/pdf"));
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("pendente").Or.Contain("PDF").Or.Contain("assinado"));
    }
}
