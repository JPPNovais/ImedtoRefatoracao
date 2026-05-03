using Imedto.Backend.Application.Estabelecimentos.Commands;
using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Estabelecimentos;

[TestFixture]
public class CriarEstabelecimentoCommandHandlerTests
{
    private Mock<IEstabelecimentoRepository> _repo;
    private Mock<IUsuarioRepository> _usuarioRepo;
    private Mock<IEventBus> _eventBus;
    private CriarEstabelecimentoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IEstabelecimentoRepository>();
        _usuarioRepo = new Mock<IUsuarioRepository>();
        _eventBus = new Mock<IEventBus>();
        _sut = new CriarEstabelecimentoCommandHandler(_repo.Object, _usuarioRepo.Object, _eventBus.Object);
    }

    private CriarEstabelecimentoCommand Cmd() => new()
    {
        DonoUsuarioId = _donoId,
        NomeFantasia = "Clinica Imedto",
        RazaoSocial = "Imedto LTDA",
        Cnpj = "12.345.678/0001-95",
        Telefone = "11999998888",
        Endereco = "Rua A, 1",
    };

    private Usuario UsuarioOnboarded()
    {
        var u = Usuario.Criar(_donoId, "joao@imedto.com");
        u.CompletarOnboarding("João", "12345678909", "11999998888");
        return u;
    }

    [Test]
    public async Task Handle_TudoValido_SalvaEPublicaEvento()
    {
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(_donoId)).ReturnsAsync(UsuarioOnboarded());
        _repo.Setup(r => r.UsuarioJaEhDono(_donoId)).ReturnsAsync(false);
        _repo.Setup(r => r.ExisteCnpj("12345678000195", 0)).ReturnsAsync(false);
        // Simula EF populando Id apos Salvar.
        _repo.Setup(r => r.Salvar(It.IsAny<Estabelecimento>()))
             .Callback<Estabelecimento>(e =>
                 typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(e, 99L))
             .Returns(Task.CompletedTask);

        await _sut.Handle(Cmd());

        _repo.Verify(r => r.Salvar(It.IsAny<Estabelecimento>()), Times.Once);
        // O handler itera DomainEvents (IDomainEvent) — Publish é chamado com tipo estatico IDomainEvent.
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is EstabelecimentoCriadoEvent)),
            Times.Once,
            "MarcarComoCriado deve enfileirar evento e o handler deve publicar.");
    }

    [Test]
    public void Handle_DonoEmpty_LancaBusinessException()
    {
        var cmd = Cmd();
        cmd.DonoUsuarioId = Guid.Empty;
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("não identificado"));
    }

    [Test]
    public void Handle_UsuarioInexistente_LancaBusinessException()
    {
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(_donoId)).ReturnsAsync((Usuario)null);
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }

    [Test]
    public void Handle_OnboardingIncompleto_LancaBusinessException()
    {
        var u = Usuario.Criar(_donoId, "joao@imedto.com"); // sem CPF
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(_donoId)).ReturnsAsync(u);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("onboarding"));
        _repo.Verify(r => r.Salvar(It.IsAny<Estabelecimento>()), Times.Never);
    }

    [Test]
    public void Handle_UsuarioJaEhDonoDeOutro_LancaBusinessException()
    {
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(_donoId)).ReturnsAsync(UsuarioOnboarded());
        _repo.Setup(r => r.UsuarioJaEhDono(_donoId)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("já é dono"));
        _repo.Verify(r => r.Salvar(It.IsAny<Estabelecimento>()), Times.Never);
    }

    [Test]
    public void Handle_CnpjDuplicado_LancaBusinessException()
    {
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(_donoId)).ReturnsAsync(UsuarioOnboarded());
        _repo.Setup(r => r.UsuarioJaEhDono(_donoId)).ReturnsAsync(false);
        _repo.Setup(r => r.ExisteCnpj("12345678000195", 0)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("CNPJ"));
        _repo.Verify(r => r.Salvar(It.IsAny<Estabelecimento>()), Times.Never);
    }

    [Test]
    public async Task Handle_SemCnpj_NaoConsultaDuplicidade()
    {
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(_donoId)).ReturnsAsync(UsuarioOnboarded());
        _repo.Setup(r => r.UsuarioJaEhDono(_donoId)).ReturnsAsync(false);
        _repo.Setup(r => r.Salvar(It.IsAny<Estabelecimento>()))
             .Callback<Estabelecimento>(e =>
                 typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(e, 99L))
             .Returns(Task.CompletedTask);

        var cmd = Cmd();
        cmd.Cnpj = null;
        await _sut.Handle(cmd);

        _repo.Verify(r => r.ExisteCnpj(It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }
}
