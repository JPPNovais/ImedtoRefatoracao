using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Imedto.Backend.API.Controllers;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Test.Controllers;

/// <summary>
/// Testes do endpoint POST /api/paciente/{pacienteId}/prontuario/anexos/base64.
/// Cobre: base64 válido aciona o AdicionarAnexoCommand (reuso do handler),
/// multi-tenant (EstabelecimentoId vem do tenant accessor, não do body) e
/// base64 malformado gera BusinessException genérica.
/// A lógica de negócio (MIME, tamanho, cross-tenant, audit LGPD) já está coberta
/// em AdicionarAnexoCommandHandlerTests — não duplica aqui.
/// </summary>
[TestFixture]
public class ProntuarioAnexoBase64ControllerTests
{
    private Mock<ICommandBus> _commandBus;
    private Mock<IRequestBus> _requestBus;
    private Mock<ICurrentTenantAccessor> _tenant;
    private ProntuarioAnexoController _sut;

    private const long EstabelecimentoId = 42;
    private const long PacienteId = 100;
    private readonly Guid _usuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _commandBus = new Mock<ICommandBus>();
        _requestBus = new Mock<IRequestBus>();
        _tenant = new Mock<ICurrentTenantAccessor>();

        _tenant.Setup(t => t.EstabelecimentoId).Returns(EstabelecimentoId);
        _tenant.Setup(t => t.UsuarioId).Returns(_usuarioId);

        _sut = new ProntuarioAnexoController(_commandBus.Object, _requestBus.Object, _tenant.Object);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private static string Base64De(string conteudo)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(conteudo));

    // ── Base64 válido: chama AdicionarAnexoCommand com os campos corretos ────

    [Test]
    public async Task UploadBase64_PayloadValido_RetornaStatus201EAcionaCommand()
    {
        var base64 = Base64De("conteudo-fake-de-imagem");
        var request = new AnexoBase64Request(
            ArquivoBase64: base64,
            NomeOriginal: "foto.png",
            MimeType: "image/png",
            EvolucaoId: 55,
            RegiaoAnatomica: "Face",
            Marcador: "Antes");

        AdicionarAnexoCommand? commandRecebido = null;
        _commandBus
            .Setup(b => b.Send(It.IsAny<AdicionarAnexoCommand>()))
            .Callback<AdicionarAnexoCommand>(c => commandRecebido = c)
            .Returns(Task.CompletedTask);

        var result = await _sut.UploadBase64(PacienteId, request);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(201));

        Assert.That(commandRecebido, Is.Not.Null);
        Assert.That(commandRecebido!.PacienteId, Is.EqualTo(PacienteId));
        Assert.That(commandRecebido.MimeType, Is.EqualTo("image/png"));
        Assert.That(commandRecebido.NomeOriginal, Is.EqualTo("foto.png"));
        Assert.That(commandRecebido.EvolucaoId, Is.EqualTo(55));
        Assert.That(commandRecebido.RegiaoAnatomica, Is.EqualTo("Face"));
        Assert.That(commandRecebido.Marcador, Is.EqualTo("Antes"));

        // Prova que os bytes decodificados chegaram corretamente ao command.
        using var ms = new MemoryStream();
        await commandRecebido.Conteudo.CopyToAsync(ms);
        Assert.That(ms.ToArray(), Is.EqualTo(Convert.FromBase64String(base64)));
    }

    // ── Multi-tenant: EstabelecimentoId e AutorUsuarioId vêm do tenant, nunca do body ──

    [Test]
    public async Task UploadBase64_MultiTenant_EstabelecimentoIdVemDoTenantNaoDoBody()
    {
        var base64 = Base64De("bytes");
        var request = new AnexoBase64Request(base64, "f.png", "image/png");

        AdicionarAnexoCommand? commandRecebido = null;
        _commandBus
            .Setup(b => b.Send(It.IsAny<AdicionarAnexoCommand>()))
            .Callback<AdicionarAnexoCommand>(c => commandRecebido = c)
            .Returns(Task.CompletedTask);

        await _sut.UploadBase64(PacienteId, request);

        Assert.That(commandRecebido!.EstabelecimentoId, Is.EqualTo(EstabelecimentoId),
            "EstabelecimentoId deve vir do ICurrentTenantAccessor, nao do payload.");
        Assert.That(commandRecebido.AutorUsuarioId, Is.EqualTo(_usuarioId),
            "AutorUsuarioId deve vir do ICurrentTenantAccessor, nao do payload.");
    }

    // ── Base64 malformado: BusinessException genérica (sem vazar detalhes técnicos) ──

    [Test]
    public void UploadBase64_Base64Malformado_LancaBusinessException()
    {
        var request = new AnexoBase64Request(
            ArquivoBase64: "NAO_E_BASE64!!!###",
            NomeOriginal: "foto.png",
            MimeType: "image/png");

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.UploadBase64(PacienteId, request));

        Assert.That(ex!.Message, Is.EqualTo("Conteúdo do arquivo inválido."));

        _commandBus.Verify(b => b.Send(It.IsAny<AdicionarAnexoCommand>()), Times.Never,
            "Command nao deve ser acionado quando base64 e invalido.");
    }

    // ── Base64 de conteúdo vazio resulta em BusinessException ─────────────────

    [Test]
    public void UploadBase64_Base64Vazio_LancaBusinessException()
    {
        // string.Empty é base64 válido de zero bytes — deve ser rejeitado como "arquivo vazio".
        var request = new AnexoBase64Request(
            ArquivoBase64: string.Empty,
            NomeOriginal: "foto.png",
            MimeType: "image/png");

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.UploadBase64(PacienteId, request));

        Assert.That(ex!.Message, Is.EqualTo("Arquivo vazio."));

        _commandBus.Verify(b => b.Send(It.IsAny<AdicionarAnexoCommand>()), Times.Never);
    }

    // ── TamanhoBytes é derivado dos bytes decodificados, não de campo do body ─

    [Test]
    public async Task UploadBase64_TamanhoBytesCalculadoDoConteudo()
    {
        var bytesOriginais = new byte[] { 1, 2, 3, 4, 5 };
        var base64 = Convert.ToBase64String(bytesOriginais);
        var request = new AnexoBase64Request(base64, "dado.bin", "image/png");

        AdicionarAnexoCommand? commandRecebido = null;
        _commandBus
            .Setup(b => b.Send(It.IsAny<AdicionarAnexoCommand>()))
            .Callback<AdicionarAnexoCommand>(c => commandRecebido = c)
            .Returns(Task.CompletedTask);

        await _sut.UploadBase64(PacienteId, request);

        Assert.That(commandRecebido!.TamanhoBytes, Is.EqualTo(bytesOriginais.Length),
            "TamanhoBytes deve refletir o numero real de bytes decodificados.");
    }
}
