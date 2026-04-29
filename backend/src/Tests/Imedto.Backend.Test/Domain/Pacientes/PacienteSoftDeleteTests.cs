using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Pacientes;

[TestFixture]
public class PacienteSoftDeleteTests
{
    private static Paciente CriarPaciente() =>
        Paciente.Cadastrar(
            estabelecimentoId: 1,
            nomeCompleto: "João Silva",
            cpf: null,
            dataNascimento: null,
            genero: GeneroPaciente.Masculino,
            telefone: null,
            email: null,
            endereco: null,
            observacoes: null);

    // --- Paciente ---

    [Test]
    public void Paciente_MarcarComoDeletado_UsuarioValido_SetaDeletadoEmEDeletadoPorUsuarioId()
    {
        var paciente = CriarPaciente();
        var usuario = Guid.NewGuid();
        var antes = DateTime.UtcNow;

        paciente.MarcarComoDeletado(usuario);

        Assert.That(paciente.DeletadoEm, Is.Not.Null);
        Assert.That(paciente.DeletadoEm!.Value, Is.GreaterThanOrEqualTo(antes));
        Assert.That(paciente.DeletadoPorUsuarioId, Is.EqualTo(usuario));
    }

    [Test]
    public void Paciente_MarcarComoDeletado_UsuarioVazio_LancaBusinessException()
    {
        var paciente = CriarPaciente();

        var ex = Assert.Throws<BusinessException>(() =>
            paciente.MarcarComoDeletado(Guid.Empty));

        Assert.That(ex.Message, Does.Contain("obrigatório").IgnoreCase);
    }

    [Test]
    public void Paciente_MarcarComoDeletado_ChamadaDuasVezes_LancaBusinessException()
    {
        var paciente = CriarPaciente();
        paciente.MarcarComoDeletado(Guid.NewGuid());

        var ex = Assert.Throws<BusinessException>(() =>
            paciente.MarcarComoDeletado(Guid.NewGuid()));

        Assert.That(ex.Message, Does.Contain("deletado").IgnoreCase);
    }

    // --- Prontuario ---

    [Test]
    public void Prontuario_MarcarComoDeletado_UsuarioValido_SetaDeletadoEm()
    {
        var prontuario = Prontuario.Iniciar(pacienteId: 1, estabelecimentoId: 1, modeloDeProntuarioId: 1);
        var usuario = Guid.NewGuid();

        prontuario.MarcarComoDeletado(usuario);

        Assert.That(prontuario.DeletadoEm, Is.Not.Null);
        Assert.That(prontuario.DeletadoPorUsuarioId, Is.EqualTo(usuario));
    }

    [Test]
    public void Prontuario_MarcarComoDeletado_UsuarioVazio_LancaBusinessException()
    {
        var prontuario = Prontuario.Iniciar(1, 1, 1);

        var ex = Assert.Throws<BusinessException>(() =>
            prontuario.MarcarComoDeletado(Guid.Empty));

        Assert.That(ex.Message, Does.Contain("obrigatório").IgnoreCase);
    }

    [Test]
    public void Prontuario_MarcarComoDeletado_ChamadaDuasVezes_LancaBusinessException()
    {
        var prontuario = Prontuario.Iniciar(1, 1, 1);
        prontuario.MarcarComoDeletado(Guid.NewGuid());

        var ex = Assert.Throws<BusinessException>(() =>
            prontuario.MarcarComoDeletado(Guid.NewGuid()));

        Assert.That(ex.Message, Does.Contain("deletado").IgnoreCase);
    }

    // --- ProntuarioEvolucao ---

    [Test]
    public void ProntuarioEvolucao_MarcarComoDeletado_UsuarioValido_SetaDeletadoEm()
    {
        var evolucao = ProntuarioEvolucao.Registrar(
            prontuarioId: 1,
            autorUsuarioId: Guid.NewGuid(),
            modeloDeProntuarioIdOrigem: 1,
            modeloSnapshotJson: "{}",
            conteudoJson: "{}");
        var usuario = Guid.NewGuid();

        evolucao.MarcarComoDeletado(usuario);

        Assert.That(evolucao.DeletadoEm, Is.Not.Null);
        Assert.That(evolucao.DeletadoPorUsuarioId, Is.EqualTo(usuario));
    }

    [Test]
    public void ProntuarioEvolucao_MarcarComoDeletado_UsuarioVazio_LancaBusinessException()
    {
        var evolucao = ProntuarioEvolucao.Registrar(1, Guid.NewGuid(), 1, "{}", "{}");

        var ex = Assert.Throws<BusinessException>(() =>
            evolucao.MarcarComoDeletado(Guid.Empty));

        Assert.That(ex.Message, Does.Contain("obrigatório").IgnoreCase);
    }

    [Test]
    public void ProntuarioEvolucao_MarcarComoDeletado_ChamadaDuasVezes_LancaBusinessException()
    {
        var evolucao = ProntuarioEvolucao.Registrar(1, Guid.NewGuid(), 1, "{}", "{}");
        evolucao.MarcarComoDeletado(Guid.NewGuid());

        var ex = Assert.Throws<BusinessException>(() =>
            evolucao.MarcarComoDeletado(Guid.NewGuid()));

        Assert.That(ex.Message, Does.Contain("deletada").IgnoreCase);
    }

    // --- ProntuarioAnexo ---

    [Test]
    public void ProntuarioAnexo_MarcarComoDeletado_UsuarioValido_SetaDeletadoEm()
    {
        var anexo = ProntuarioAnexo.Registrar(
            prontuarioId: 1,
            estabelecimentoId: 1,
            evolucaoId: null,
            storagePath: "prontuarios/1/laudo.pdf",
            nomeOriginal: "laudo.pdf",
            mimeType: "application/pdf",
            tamanhoBytes: 12345,
            criadoPorUsuarioId: Guid.NewGuid());
        var usuario = Guid.NewGuid();

        anexo.MarcarComoDeletado(usuario);

        Assert.That(anexo.DeletadoEm, Is.Not.Null);
        Assert.That(anexo.DeletadoPorUsuarioId, Is.EqualTo(usuario));
    }

    [Test]
    public void ProntuarioAnexo_MarcarComoDeletado_UsuarioVazio_LancaBusinessException()
    {
        var anexo = ProntuarioAnexo.Registrar(1, 1, null, "path", "file.pdf", "application/pdf", 1000, Guid.NewGuid());

        var ex = Assert.Throws<BusinessException>(() =>
            anexo.MarcarComoDeletado(Guid.Empty));

        Assert.That(ex.Message, Does.Contain("obrigatório").IgnoreCase);
    }

    [Test]
    public void ProntuarioAnexo_MarcarComoDeletado_ChamadaDuasVezes_LancaBusinessException()
    {
        var anexo = ProntuarioAnexo.Registrar(1, 1, null, "path", "file.pdf", "application/pdf", 1000, Guid.NewGuid());
        anexo.MarcarComoDeletado(Guid.NewGuid());

        var ex = Assert.Throws<BusinessException>(() =>
            anexo.MarcarComoDeletado(Guid.NewGuid()));

        Assert.That(ex.Message, Does.Contain("deletado").IgnoreCase);
    }
}
