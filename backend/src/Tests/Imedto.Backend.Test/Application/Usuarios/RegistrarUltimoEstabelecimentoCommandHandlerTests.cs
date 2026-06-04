using Imedto.Backend.Application.Usuarios.Commands;
using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Usuarios;

[TestFixture]
public class RegistrarUltimoEstabelecimentoCommandHandlerTests
{
    private Mock<IUsuarioRepository> _usuarioRepo;
    private Mock<IVinculoRepository> _vinculoRepo;
    private CurrentTenantAccessor _tenant;
    private RegistrarUltimoEstabelecimentoCommandHandler _sut;
    private readonly Guid _usuarioId = Guid.NewGuid();
    private const long EstabelecimentoId = 42L;

    [SetUp]
    public void SetUp()
    {
        _usuarioRepo = new Mock<IUsuarioRepository>();
        _vinculoRepo = new Mock<IVinculoRepository>();
        _tenant = new CurrentTenantAccessor();
        _sut = new RegistrarUltimoEstabelecimentoCommandHandler(
            _usuarioRepo.Object,
            _vinculoRepo.Object,
            _tenant);
    }

    private RegistrarUltimoEstabelecimentoCommand Cmd() =>
        new() { EstabelecimentoId = EstabelecimentoId };

    private Usuario UsuarioOnboarded()
    {
        var u = Usuario.Criar(_usuarioId, "joao@imedto.com");
        u.CompletarOnboarding("João", "12345678909", "11999998888");
        return u;
    }

    [Test]
    public async Task Handle_UsuarioComAcesso_GravaUltimoEstabelecimento()
    {
        _tenant.DefinirUsuario(_usuarioId);
        _vinculoRepo.Setup(r => r.PodeAtuarComoProfissional(_usuarioId, EstabelecimentoId))
            .ReturnsAsync(true);
        var usuario = UsuarioOnboarded();
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(usuario);

        await _sut.Handle(Cmd());

        Assert.That(usuario.UltimoEstabelecimentoId, Is.EqualTo(EstabelecimentoId));
        _usuarioRepo.Verify(r => r.Salvar(usuario), Times.Once);
    }

    [Test]
    public void Handle_UsuarioSemAcesso_LancaBusinessException()
    {
        // Multi-tenant falha-fechada: usuário sem vínculo ou sem ser Dono → exceção genérica.
        _tenant.DefinirUsuario(_usuarioId);
        _vinculoRepo.Setup(r => r.PodeAtuarComoProfissional(_usuarioId, EstabelecimentoId))
            .ReturnsAsync(false);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Não encontrado."));
        _usuarioRepo.Verify(r => r.Salvar(It.IsAny<Usuario>()), Times.Never);
    }

    [Test]
    public void Handle_UsuarioNaoAutenticado_LancaBusinessException()
    {
        // tenant.UsuarioId é Guid.Empty sem DefinirUsuario.
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não autenticado"));
        _vinculoRepo.Verify(r => r.PodeAtuarComoProfissional(It.IsAny<Guid>(), It.IsAny<long>()), Times.Never);
    }

    [Test]
    public void Handle_UsuarioComAcesso_MasNaoEncontradoNoBanco_LancaBusinessException()
    {
        _tenant.DefinirUsuario(_usuarioId);
        _vinculoRepo.Setup(r => r.PodeAtuarComoProfissional(_usuarioId, EstabelecimentoId))
            .ReturnsAsync(true);
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync((Usuario)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
        _usuarioRepo.Verify(r => r.Salvar(It.IsAny<Usuario>()), Times.Never);
    }
}
