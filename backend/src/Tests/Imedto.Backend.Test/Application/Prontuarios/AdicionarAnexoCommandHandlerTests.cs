using System.Text;
using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Storage;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

[TestFixture]
public class AdicionarAnexoCommandHandlerTests
{
    private Mock<IProntuarioRepository> _prontuarioRepo;
    private Mock<IProntuarioAnexoRepository> _anexoRepo;
    private Mock<IAnexoStorageService> _storage;
    private Mock<IPacienteRepository> _pacienteRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private AdicionarAnexoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 100;
    private const long ProntuarioId = 200;
    private readonly Guid _autorId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _prontuarioRepo = new Mock<IProntuarioRepository>();
        _anexoRepo = new Mock<IProntuarioAnexoRepository>();
        _storage = new Mock<IAnexoStorageService>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();

        var opts = Options.Create(new StorageOptions
        {
            TamanhoMaxMb = 10,
            MimeTypesPermitidos = new[] { "image/png", "application/pdf" },
        });

        _sut = new AdicionarAnexoCommandHandler(
            _prontuarioRepo.Object, _anexoRepo.Object, _storage.Object,
            _pacienteRepo.Object, _acessoLog.Object, opts);
    }

    private static Paciente PacienteAtivo()
    {
        var p = Paciente.Cadastrar(EstabelecimentoId, "P", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, PacienteId);
        return p;
    }

    private static Prontuario ProntuarioJaIniciado()
    {
        var p = Prontuario.Iniciar(PacienteId, EstabelecimentoId, 1L);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, ProntuarioId);
        return p;
    }

    private AdicionarAnexoCommand Cmd(
        string mime = "image/png",
        long tamanho = 1024,
        string? regiaoAnatomica = null,
        string? marcador = null) => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabelecimentoId,
        AutorUsuarioId = _autorId,
        NomeOriginal = "foto.png",
        MimeType = mime,
        TamanhoBytes = tamanho,
        Conteudo = new MemoryStream(Encoding.UTF8.GetBytes("bytes")),
        RegiaoAnatomica = regiaoAnatomica,
        Marcador = marcador,
    };

    [Test]
    public async Task Handle_TudoValido_SobeArquivoEPersisteAnexoEAudita()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioJaIniciado());

        await _sut.Handle(Cmd());

        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(),
            It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()),
            Times.Once);
        _anexoRepo.Verify(r => r.Salvar(It.IsAny<ProntuarioAnexo>()), Times.Once);
        _acessoLog.Verify(a => a.RegistrarAsync(
            ProntuarioId, _autorId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
    }

    [Test]
    public void Handle_PacienteCrossTenant_LancaMensagemGenericaENaoToca()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync((Paciente)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."));
        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(),
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Storage nao deve receber upload se paciente nao foi validado.");
    }

    [Test]
    public void Handle_SemProntuario_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync((Prontuario)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("prontuário"));
        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(),
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void Handle_TamanhoZero_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioJaIniciado());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(tamanho: 0)));
        Assert.That(ex.Message, Does.Contain("Tamanho"));
    }

    [Test]
    public void Handle_TamanhoAcimaDoLimite_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioJaIniciado());

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.Handle(Cmd(tamanho: 11L * 1024 * 1024)));
        Assert.That(ex.Message, Does.Contain("Tamanho"));
        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(),
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void Handle_MimeTypeNaoPermitido_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioJaIniciado());

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.Handle(Cmd(mime: "application/zip")));
        Assert.That(ex.Message, Does.Contain("não permitido"));
        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(),
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_ComRegiaoEMarcador_PersisteCamposNoAnexo()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioJaIniciado());

        ProntuarioAnexo? anexoSalvo = null;
        _anexoRepo.Setup(r => r.Salvar(It.IsAny<ProntuarioAnexo>()))
                  .Callback<ProntuarioAnexo>(a => anexoSalvo = a)
                  .Returns(Task.CompletedTask);

        await _sut.Handle(Cmd(regiaoAnatomica: "Face", marcador: "Antes"));

        Assert.That(anexoSalvo, Is.Not.Null);
        Assert.That(anexoSalvo!.RegiaoAnatomica, Is.EqualTo("Face"));
        Assert.That(anexoSalvo.Marcador, Is.EqualTo("Antes"));
    }

    [Test]
    public async Task Handle_SemRegiaoNemMarcador_AnexoAntigoSegueFuncionando()
    {
        // Retrocompatibilidade: anexo genérico (PDF, doc) sem metadados de foto clínica.
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioJaIniciado());

        ProntuarioAnexo? anexoSalvo = null;
        _anexoRepo.Setup(r => r.Salvar(It.IsAny<ProntuarioAnexo>()))
                  .Callback<ProntuarioAnexo>(a => anexoSalvo = a)
                  .Returns(Task.CompletedTask);

        await _sut.Handle(Cmd(mime: "application/pdf"));

        Assert.That(anexoSalvo, Is.Not.Null);
        Assert.That(anexoSalvo!.RegiaoAnatomica, Is.Null);
        Assert.That(anexoSalvo.Marcador, Is.Null);
    }
}

/// <summary>
/// Testes adicionais para CA2-CA4 do briefing 2026-06-27_002:
/// whitelist Office, limite 2MB para marcador "anexo"/"foto-paciente".
/// </summary>
[TestFixture]
public class AdicionarAnexo_Briefing002_Tests
{
    private Mock<IProntuarioRepository> _prontuarioRepo;
    private Mock<IProntuarioAnexoRepository> _anexoRepo;
    private Mock<IAnexoStorageService> _storage;
    private Mock<IPacienteRepository> _pacienteRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 100;
    private const long ProntuarioId = 200;
    private readonly Guid _autorId = Guid.NewGuid();

    private static readonly string[] TodosMimeTypes =
    [
        "application/pdf",
        "image/png", "image/jpeg", "image/webp",
        "application/dicom",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    ];

    private AdicionarAnexoCommandHandler CriarSut(int tamanhoMaxMb = 50, int tamanhoMaxAnexoMb = 2) =>
        new AdicionarAnexoCommandHandler(
            _prontuarioRepo.Object, _anexoRepo.Object, _storage.Object,
            _pacienteRepo.Object, _acessoLog.Object,
            Options.Create(new StorageOptions
            {
                TamanhoMaxMb = tamanhoMaxMb,
                TamanhoMaxAnexoMb = tamanhoMaxAnexoMb,
                MimeTypesPermitidos = TodosMimeTypes,
            }));

    [SetUp]
    public void SetUp()
    {
        _prontuarioRepo = new Mock<IProntuarioRepository>();
        _anexoRepo = new Mock<IProntuarioAnexoRepository>();
        _storage = new Mock<IAnexoStorageService>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();

        var paciente = Paciente.Cadastrar(EstabelecimentoId, "P", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(paciente, PacienteId);
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(paciente);

        var prontuario = Prontuario.Iniciar(PacienteId, EstabelecimentoId, 1L);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(prontuario, ProntuarioId);
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId)).ReturnsAsync(prontuario);
    }

    [TestCase("application/msword")]
    [TestCase("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [TestCase("application/vnd.ms-excel")]
    [TestCase("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task Handle_OfficeAceito_QuandoWhitelistContemMime(string mime)
    {
        // CA2: Office (.doc/.docx/.xls/.xlsx) aceito pela whitelist.
        var sut = CriarSut();
        var cmd = new AdicionarAnexoCommand
        {
            PacienteId = PacienteId, EstabelecimentoId = EstabelecimentoId,
            AutorUsuarioId = _autorId, NomeOriginal = "documento.docx",
            MimeType = mime, TamanhoBytes = 500_000, // 500 KB
            Conteudo = new MemoryStream([1, 2, 3]),
            Marcador = "anexo",
        };

        await sut.Handle(cmd);

        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(),
            It.IsAny<Stream>(), mime, It.IsAny<CancellationToken>()), Times.Once,
            $"MIME {mime} deve ser aceito pela whitelist após briefing 002.");
    }

    [TestCase("application/zip")]
    [TestCase("application/x-executable")]
    [TestCase("text/javascript")]
    public void Handle_TipoInvalido_LancaBusinessException(string mime)
    {
        // CA3: tipos não permitidos rejeitados com 422 genérico.
        var sut = CriarSut();
        var cmd = new AdicionarAnexoCommand
        {
            PacienteId = PacienteId, EstabelecimentoId = EstabelecimentoId,
            AutorUsuarioId = _autorId, NomeOriginal = "malicioso.zip",
            MimeType = mime, TamanhoBytes = 1024,
            Conteudo = new MemoryStream([1]),
            Marcador = "anexo",
        };

        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("não permitido"), $"CA3: MIME {mime} deve ser rejeitado.");
    }

    [Test]
    public void Handle_MarcadorAnexo_Mais2MB_LancaBusinessException()
    {
        // CA4: anexo de prontuário > 2MB rejeitado.
        var sut = CriarSut(); // limite padrão 2MB para Marcador="anexo"
        var cmd = new AdicionarAnexoCommand
        {
            PacienteId = PacienteId, EstabelecimentoId = EstabelecimentoId,
            AutorUsuarioId = _autorId, NomeOriginal = "laudo.pdf",
            MimeType = "application/pdf",
            TamanhoBytes = 3L * 1024 * 1024, // 3 MB — acima do limite da seção
            Conteudo = new MemoryStream([1]),
            Marcador = "anexo",
        };

        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("inválido"),
            "CA4: anexo >2MB deve ser rejeitado antes de subir ao S3.");
        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(),
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never, "S3 não deve receber arquivo acima do limite.");
    }

    [Test]
    public void Handle_MarcadorFotoPaciente_Mais2MB_LancaBusinessException()
    {
        // CA4/CA6: foto-paciente também tem limite 2MB (após redimensionamento a 1600px
        // sempre fica <1MB no front, mas o back valida como defense-in-depth).
        var sut = CriarSut();
        var cmd = new AdicionarAnexoCommand
        {
            PacienteId = PacienteId, EstabelecimentoId = EstabelecimentoId,
            AutorUsuarioId = _autorId, NomeOriginal = "foto-grande.jpg",
            MimeType = "image/jpeg",
            TamanhoBytes = 3L * 1024 * 1024, // 3 MB
            Conteudo = new MemoryStream([1]),
            Marcador = "foto-paciente",
        };

        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("inválido"));
    }

    [Test]
    public async Task Handle_SemMarcador_UsaLimiteGlobal_NaoRejeita2MB()
    {
        // Retrocompat: anexos sem marcador (ex.: migração, exame físico) usam limite global 50MB.
        var sut = CriarSut(tamanhoMaxMb: 50);
        var cmd = new AdicionarAnexoCommand
        {
            PacienteId = PacienteId, EstabelecimentoId = EstabelecimentoId,
            AutorUsuarioId = _autorId, NomeOriginal = "grande.pdf",
            MimeType = "application/pdf",
            TamanhoBytes = 3L * 1024 * 1024, // 3 MB — acima de 2MB mas sem marcador de seção
            Conteudo = new MemoryStream([1]),
            Marcador = null, // sem marcador → limite global
        };

        await sut.Handle(cmd);

        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(),
            It.IsAny<Stream>(), "application/pdf", It.IsAny<CancellationToken>()), Times.Once,
            "Anexo sem marcador de seção usa limite global, não deve ser rejeitado por 3MB.");
    }
}
