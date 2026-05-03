using Imedto.Backend.Application.Profissionais.Commands;
using Imedto.Backend.Contracts.Profissionais.Commands;
using Imedto.Backend.Domain.Profissionais;
using Imedto.Backend.Domain.Profissionais.Events;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Profissionais;

[TestFixture]
public class CadastrarProfissionalCommandHandlerTests
{
    private Mock<IProfissionalRepository> _repo;
    private Mock<IUsuarioRepository> _usuarioRepo;
    private Mock<IEventBus> _eventBus;
    private CadastrarProfissionalCommandHandler _sut;
    private readonly Guid _usuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IProfissionalRepository>();
        _usuarioRepo = new Mock<IUsuarioRepository>();
        _eventBus = new Mock<IEventBus>();
        _sut = new CadastrarProfissionalCommandHandler(_repo.Object, _usuarioRepo.Object, _eventBus.Object);
    }

    private Usuario UsuarioOnboarded()
    {
        var u = Usuario.Criar(_usuarioId, "p@imedto.com");
        u.CompletarOnboarding("Profissional", "12345678909", null);
        return u;
    }

    private CadastrarProfissionalCommand Cmd(Guid? id = null) => new()
    {
        UsuarioId = id ?? _usuarioId,
        Conselho = "CRM",
        Uf = "SP",
        NumeroRegistro = "12345",
        Especialidade = "Cardiologia",
        Bio = null,
    };

    [Test]
    public async Task Handle_TudoValido_PersisteEPublicaEvento()
    {
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(UsuarioOnboarded());
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync((Profissional)null);
        _repo.Setup(r => r.ExisteConselhoRegistro("CRM", "SP", "12345", _usuarioId)).ReturnsAsync(false);

        await _sut.Handle(Cmd());

        _repo.Verify(r => r.Salvar(It.IsAny<Profissional>()), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is ProfissionalCadastradoEvent)),
            Times.Once);
    }

    [Test]
    public void Handle_UsuarioGuidEmpty_LancaBusinessException()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(id: Guid.Empty)));
        Assert.That(ex.Message, Does.Contain("não identificado"));
    }

    [Test]
    public void Handle_UsuarioInexistente_LancaBusinessException()
    {
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync((Usuario)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }

    [Test]
    public void Handle_OnboardingIncompleto_LancaBusinessException()
    {
        var u = Usuario.Criar(_usuarioId, "p@imedto.com"); // sem CPF
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(u);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("onboarding"));
    }

    [Test]
    public void Handle_JaPossuiCadastroProfissional_LancaBusinessException()
    {
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(UsuarioOnboarded());
        var existente = Profissional.Cadastrar(_usuarioId, "CRM", "SP", "1", null, null);
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(existente);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("já possui"));
    }

    [Test]
    public void Handle_RegistroJaUsadoPorOutroProfissional_LancaBusinessException()
    {
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(UsuarioOnboarded());
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync((Profissional)null);
        _repo.Setup(r => r.ExisteConselhoRegistro("CRM", "SP", "12345", _usuarioId)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("registro"));
    }
}
