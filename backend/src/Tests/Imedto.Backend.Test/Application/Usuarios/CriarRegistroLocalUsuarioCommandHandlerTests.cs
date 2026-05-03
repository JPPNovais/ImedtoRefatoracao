using Imedto.Backend.Application.Usuarios.Commands;
using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Usuarios.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Usuarios;

[TestFixture]
public class CriarRegistroLocalUsuarioCommandHandlerTests
{
    private Mock<IUsuarioRepository> _repo;
    private Mock<IEventBus> _eventBus;
    private CriarRegistroLocalUsuarioCommandHandler _sut;
    private readonly Guid _usuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IUsuarioRepository>();
        _eventBus = new Mock<IEventBus>();
        _sut = new CriarRegistroLocalUsuarioCommandHandler(_repo.Object, _eventBus.Object);
    }

    private CriarRegistroLocalUsuarioCommand Cmd() => new()
    {
        Id = _usuarioId,
        Email = "novo@imedto.com",
    };

    [Test]
    public async Task Handle_UsuarioInexistente_CriaRegistroEPublicaEvento()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync((Usuario)null);

        await _sut.Handle(Cmd());

        _repo.Verify(r => r.Salvar(It.Is<Usuario>(u =>
            u.Id == _usuarioId && u.Email == "novo@imedto.com")),
            Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is UsuarioCriadoEvent)),
            Times.Once);
    }

    [Test]
    public async Task Handle_UsuarioJaExiste_RegistraAcessoEDoNotCriarNovo()
    {
        var existente = Usuario.Criar(_usuarioId, "antigo@imedto.com");
        existente.ClearDomainEvents(); // limpar evento de Criar
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(existente);

        await _sut.Handle(Cmd());

        Assert.That(existente.UltimoAcessoEm, Is.Not.Null,
            "RegistrarAcesso deve atualizar UltimoAcessoEm.");
        _repo.Verify(r => r.Salvar(existente), Times.Once);
        _eventBus.Verify(b => b.Publish(It.IsAny<IDomainEvent>()), Times.Never,
            "Idempotência: usuário já existente nao gera evento de Criado novamente.");
    }
}
