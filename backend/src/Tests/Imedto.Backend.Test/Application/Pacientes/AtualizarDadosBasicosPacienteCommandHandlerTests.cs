using Imedto.Backend.Application.Pacientes.Commands;
using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Pacientes;

/// <summary>
/// Prova que AtualizarDadosBasicosPacienteCommandHandler é uma atualização PARCIAL:
/// campos não enviados são preservados (especialmente alertas, telefone, observações).
/// </summary>
[TestFixture]
public class AtualizarDadosBasicosPacienteCommandHandlerTests
{
    private Mock<IPacienteRepository> _repo;
    private Mock<IPacienteAcessoLogService> _acessoLog;
    private AtualizarDadosBasicosPacienteCommandHandler _sut;

    private const long EstabelecimentoId = 10;
    private const long PacienteId = 55;
    private readonly Guid _solicitante = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IPacienteRepository>();
        _acessoLog = new Mock<IPacienteAcessoLogService>();
        _sut = new AtualizarDadosBasicosPacienteCommandHandler(_repo.Object, _acessoLog.Object);
    }

    /// <summary>
    /// Cria um paciente com todos os campos preenchidos para provar que os não enviados sobrevivem.
    /// </summary>
    private static Paciente CriarPacienteCompleto()
    {
        var p = Paciente.Cadastrar(
            EstabelecimentoId,
            "Nome Original",
            "12345678909",
            new DateTime(1990, 5, 15),
            GeneroPaciente.Feminino,
            "11999991111",
            "original@email.com",
            "Rua Original, 1",
            "Observação importante",
            documentoInternacional: null,
            tags: new[] { "vip" },
            alertas: new[] { "Alergia grave a penicilina" });
        return p;
    }

    // ── CA principal: enviar só nome, preservar todo o resto ────────────────

    [Test]
    public async Task Handle_SoNome_AlteraNomeEPreservaTudoMais()
    {
        var paciente = CriarPacienteCompleto();
        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(paciente);

        await _sut.Handle(new AtualizarDadosBasicosPacienteCommand
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitante,
            NomeCompleto = "Nome Alterado",
            // campos não enviados = null / flags false
        });

        Assert.That(paciente.NomeCompleto, Is.EqualTo("Nome Alterado"));
        // Preservados:
        Assert.That(paciente.Telefone, Is.EqualTo("11999991111"));
        Assert.That(paciente.Cpf, Is.EqualTo("12345678909"));
        Assert.That(paciente.Observacoes, Is.EqualTo("Observação importante"));
        Assert.That(paciente.Alertas, Has.Count.EqualTo(1));
        Assert.That(paciente.Alertas[0], Is.EqualTo("Alergia grave a penicilina"));
        Assert.That(paciente.Tags, Has.Count.EqualTo(1));
        Assert.That(paciente.Genero, Is.EqualTo(GeneroPaciente.Feminino));
        Assert.That(paciente.Endereco, Is.EqualTo("Rua Original, 1"));
        _repo.Verify(r => r.Salvar(paciente), Times.Once);
        _acessoLog.Verify(a => a.RegistrarAsync(
            PacienteId, _solicitante, EstabelecimentoId, TipoAcessoPaciente.Edicao), Times.Once);
    }

    // ── Enviar só telefone ────────────────────────────────────────────────────

    [Test]
    public async Task Handle_SoTelefone_AlteraTelefoneEPreserveNomeEAlertas()
    {
        var paciente = CriarPacienteCompleto();
        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(paciente);

        await _sut.Handle(new AtualizarDadosBasicosPacienteCommand
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitante,
            Telefone = "11888887777",
        });

        Assert.That(paciente.Telefone, Is.EqualTo("11888887777"));
        Assert.That(paciente.NomeCompleto, Is.EqualTo("Nome Original"));
        Assert.That(paciente.Alertas, Has.Count.EqualTo(1));
    }

    // ── Multi-tenant: paciente de outro estab retorna mensagem genérica ───────

    [Test]
    public void Handle_PacienteDeOutroTenant_LancaMensagemGenerica()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync((Paciente)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new AtualizarDadosBasicosPacienteCommand
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitante,
            NomeCompleto = "Qualquer",
        }));

        Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."));
        _repo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Never);
        _acessoLog.Verify(a => a.RegistrarAsync(
            It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<TipoAcessoPaciente>()),
            Times.Never);
    }

    // ── CPF duplicado → 422 ──────────────────────────────────────────────────

    [Test]
    public void Handle_CpfDuplicadoEmOutroPaciente_LancaBusinessException()
    {
        var paciente = CriarPacienteCompleto();
        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(paciente);
        _repo.Setup(r => r.ExisteCpfNoEstabelecimento("98765432100", EstabelecimentoId, PacienteId))
             .ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new AtualizarDadosBasicosPacienteCommand
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitante,
            Cpf = "987.654.321-00",
            CpfFoiEnviado = true,
        }));

        Assert.That(ex.Message, Does.Contain("CPF"));
        _repo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Never);
    }

    // ── Nome vazio → 422 ─────────────────────────────────────────────────────

    [Test]
    public void Handle_NomeVazio_LancaBusinessException()
    {
        var paciente = CriarPacienteCompleto();
        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(paciente);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new AtualizarDadosBasicosPacienteCommand
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitante,
            NomeCompleto = "   ", // whitespace = inválido
        }));

        Assert.That(ex.Message, Does.Contain("Nome"));
        _repo.Verify(r => r.Salvar(It.IsAny<Paciente>()), Times.Never);
    }

    // ── DataNascimentoFoiEnviada=false → não altera data existente ───────────

    [Test]
    public async Task Handle_DataNascimentoNaoEnviada_MantemDataExistente()
    {
        var paciente = CriarPacienteCompleto();
        var dataOriginal = paciente.DataNascimento;

        _repo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(paciente);

        // Envia só o email, sem flag de data
        await _sut.Handle(new AtualizarDadosBasicosPacienteCommand
        {
            PacienteId = PacienteId,
            EstabelecimentoId = EstabelecimentoId,
            SolicitanteUsuarioId = _solicitante,
            Email = "novo@email.com",
            DataNascimento = null,
            DataNascimentoFoiEnviada = false, // flag false = ignorar campo
        });

        Assert.That(paciente.DataNascimento, Is.EqualTo(dataOriginal));
        Assert.That(paciente.Email, Is.EqualTo("novo@email.com"));
    }
}
