using Imedto.Backend.Application.Profissionais.Commands;
using Imedto.Backend.Contracts.Profissionais.Commands;
using Imedto.Backend.Domain.Profissionais;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Profissionais;

[TestFixture]
public class AtualizarProfissionalCommandHandlerTests
{
    private Mock<IProfissionalRepository> _repo;
    private AtualizarProfissionalCommandHandler _sut;
    private readonly Guid _usuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IProfissionalRepository>();
        _sut = new AtualizarProfissionalCommandHandler(_repo.Object);
    }

    private Profissional Profissional() =>
        Imedto.Backend.Domain.Profissionais.Profissional.Cadastrar(
            _usuarioId, "CRM", "SP", "1", null, null);

    private AtualizarProfissionalCommand Cmd(Guid? id = null) => new()
    {
        UsuarioId = id ?? _usuarioId,
        Conselho = "CRO",
        Uf = "RJ",
        NumeroRegistro = "999",
        Especialidade = "Ortodontia",
        Bio = "Nova bio",
    };

    [Test]
    public async Task Handle_TudoValido_AtualizaCadastro()
    {
        var prof = Profissional();
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(prof);
        _repo.Setup(r => r.ExisteConselhoRegistro("CRO", "RJ", "999", _usuarioId)).ReturnsAsync(false);

        await _sut.Handle(Cmd());

        Assert.That(prof.Conselho, Is.EqualTo("CRO"));
        Assert.That(prof.Uf, Is.EqualTo("RJ"));
        Assert.That(prof.NumeroRegistro, Is.EqualTo("999"));
        _repo.Verify(r => r.Salvar(prof), Times.Once);
    }

    [Test]
    public void Handle_UsuarioGuidEmpty_LancaBusinessException()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(id: Guid.Empty)));
        Assert.That(ex.Message, Does.Contain("não identificado"));
    }

    [Test]
    public void Handle_CadastroInexistente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync((Profissional)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }

    [Test]
    public void Handle_RegistroDuplicadoEmOutroUsuario_LancaBusinessException()
    {
        var prof = Profissional();
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(prof);
        _repo.Setup(r => r.ExisteConselhoRegistro("CRO", "RJ", "999", _usuarioId)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("registro"));
        _repo.Verify(r => r.Salvar(It.IsAny<Profissional>()), Times.Never);
    }
}
