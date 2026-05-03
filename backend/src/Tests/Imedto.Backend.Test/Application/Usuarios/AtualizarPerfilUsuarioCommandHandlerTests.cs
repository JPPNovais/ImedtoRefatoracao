using Imedto.Backend.Application.Usuarios.Commands;
using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Usuarios;

[TestFixture]
public class AtualizarPerfilUsuarioCommandHandlerTests
{
    private Mock<IUsuarioRepository> _repo;
    private CurrentTenantAccessor _tenant;
    private AtualizarPerfilUsuarioCommandHandler _sut;
    private readonly Guid _usuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IUsuarioRepository>();
        _tenant = new CurrentTenantAccessor();
        _sut = new AtualizarPerfilUsuarioCommandHandler(_repo.Object, _tenant);
    }

    private static AtualizarPerfilUsuarioCommand Cmd(Guid? usuarioIdNoBody = null) =>
        new()
        {
            UsuarioId = usuarioIdNoBody ?? Guid.NewGuid(), // diferente do JWT
            NomeCompleto = "João Silva",
            Telefone = "11888887777",
        };

    private Usuario UsuarioOnboarded()
    {
        var u = Usuario.Criar(_usuarioId, "joao@imedto.com");
        u.CompletarOnboarding("João", "12345678909", "11999998888");
        return u;
    }

    [Test]
    public async Task Handle_UsuarioJwtValido_AtualizaPerfilEUsaIdDoJwt()
    {
        _tenant.DefinirUsuario(_usuarioId);
        var usuario = UsuarioOnboarded();
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(usuario);

        await _sut.Handle(Cmd(usuarioIdNoBody: Guid.NewGuid()));

        _repo.Verify(r => r.ObterPorIdOuNulo(_usuarioId), Times.Once,
            "Handler deve buscar pelo Id do JWT, ignorando o do body.");
        _repo.Verify(r => r.Salvar(usuario), Times.Once);
        Assert.That(usuario.NomeCompleto, Is.EqualTo("João Silva"));
        Assert.That(usuario.Telefone, Is.EqualTo("11888887777"));
        Assert.That(usuario.Cpf, Is.EqualTo("12345678909"), "AtualizarPerfil nao deve mexer no CPF.");
    }

    [Test]
    public void Handle_UsuarioNaoAutenticado_LancaBusinessException()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não autenticado"));
    }

    [Test]
    public void Handle_UsuarioNaoEncontrado_LancaBusinessException()
    {
        _tenant.DefinirUsuario(_usuarioId);
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync((Usuario)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
        _repo.Verify(r => r.Salvar(It.IsAny<Usuario>()), Times.Never);
    }
}
