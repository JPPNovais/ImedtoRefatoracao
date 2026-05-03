using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Prontuarios;

[TestFixture]
public class ProntuarioAnexoTests
{
    private static ProntuarioAnexo CriarValido() =>
        ProntuarioAnexo.Registrar(
            prontuarioId: 1L,
            estabelecimentoId: 10L,
            evolucaoId: null,
            storagePath: "anexos/1/foto.jpg",
            nomeOriginal: "foto.jpg",
            mimeType: "image/jpeg",
            tamanhoBytes: 1024,
            criadoPorUsuarioId: Guid.NewGuid());

    // ----- Registrar -----

    [Test]
    public void Registrar_Valido_StateOk()
    {
        var a = CriarValido();
        Assert.That(a.ProntuarioId, Is.EqualTo(1L));
        Assert.That(a.EstabelecimentoId, Is.EqualTo(10L));
        Assert.That(a.NomeOriginal, Is.EqualTo("foto.jpg"));
        Assert.That(a.MimeType, Is.EqualTo("image/jpeg"));
        Assert.That(a.TamanhoBytes, Is.EqualTo(1024));
        Assert.That(a.EstaArquivado, Is.False);
        Assert.That(a.DeletadoEm, Is.Null);
        Assert.That(a.CriadoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void Registrar_NomeOriginalTrimado_RemoveEspacos()
    {
        var a = ProntuarioAnexo.Registrar(1L, 10L, null, "anexos/1.jpg", "  foto.jpg  ",
            "image/jpeg", 512, Guid.NewGuid());
        Assert.That(a.NomeOriginal, Is.EqualTo("foto.jpg"));
    }

    [Test]
    public void Registrar_SemMimeType_AdotaOctetStream()
    {
        var a = ProntuarioAnexo.Registrar(1L, 10L, null, "anexos/1.bin", "x.bin",
            "  ", 100, Guid.NewGuid());
        Assert.That(a.MimeType, Is.EqualTo("application/octet-stream"));
    }

    [Test]
    public void Registrar_ProntuarioIdZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ProntuarioAnexo.Registrar(0L, 10L, null, "p/1.jpg", "x.jpg", "image/jpeg", 1, Guid.NewGuid()));
        Assert.That(ex.Message, Does.Contain("Prontuário"));
    }

    [Test]
    public void Registrar_StoragePathVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ProntuarioAnexo.Registrar(1L, 10L, null, "  ", "x.jpg", "image/jpeg", 1, Guid.NewGuid()));
        Assert.That(ex.Message, Does.Contain("Storage"));
    }

    [Test]
    public void Registrar_NomeOriginalVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ProntuarioAnexo.Registrar(1L, 10L, null, "p/1.jpg", "  ", "image/jpeg", 1, Guid.NewGuid()));
        Assert.That(ex.Message, Does.Contain("Nome original"));
    }

    [Test]
    public void Registrar_TamanhoZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ProntuarioAnexo.Registrar(1L, 10L, null, "p/1.jpg", "x.jpg", "image/jpeg", 0, Guid.NewGuid()));
        Assert.That(ex.Message, Does.Contain("Tamanho"));
    }

    [Test]
    public void Registrar_AutorEmpty_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ProntuarioAnexo.Registrar(1L, 10L, null, "p/1.jpg", "x.jpg", "image/jpeg", 1, Guid.Empty));
        Assert.That(ex.Message, Does.Contain("Autor"));
    }

    // ----- Arquivar -----

    [Test]
    public void Arquivar_Valido_SetaCamposEFlag()
    {
        var a = CriarValido();
        var quem = Guid.NewGuid();
        a.Arquivar(quem);

        Assert.That(a.EstaArquivado, Is.True);
        Assert.That(a.ArquivadoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
        Assert.That(a.ArquivadoPorUsuarioId, Is.EqualTo(quem));
    }

    [Test]
    public void Arquivar_JaArquivado_LancaBusinessException()
    {
        var a = CriarValido();
        a.Arquivar(Guid.NewGuid());
        var ex = Assert.Throws<BusinessException>(() => a.Arquivar(Guid.NewGuid()));
        Assert.That(ex.Message, Does.Contain("já está arquivado"));
    }

    // ----- MarcarComoDeletado (LGPD) -----

    [Test]
    public void MarcarComoDeletado_Valido_SetaAuditoria()
    {
        var a = CriarValido();
        var quem = Guid.NewGuid();
        a.MarcarComoDeletado(quem);

        Assert.That(a.DeletadoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
        Assert.That(a.DeletadoPorUsuarioId, Is.EqualTo(quem));
    }

    [Test]
    public void MarcarComoDeletado_UsuarioEmpty_LancaBusinessException()
    {
        var a = CriarValido();
        Assert.Throws<BusinessException>(() => a.MarcarComoDeletado(Guid.Empty));
    }

    [Test]
    public void MarcarComoDeletado_JaDeletado_LancaBusinessException()
    {
        var a = CriarValido();
        a.MarcarComoDeletado(Guid.NewGuid());
        Assert.Throws<BusinessException>(() => a.MarcarComoDeletado(Guid.NewGuid()));
    }

    [Test]
    public void Arquivar_E_MarcarComoDeletado_SaoEstadosIndependentes()
    {
        var a = CriarValido();
        a.Arquivar(Guid.NewGuid());
        a.MarcarComoDeletado(Guid.NewGuid());

        Assert.That(a.EstaArquivado, Is.True);
        Assert.That(a.DeletadoEm, Is.Not.Null);
    }
}
