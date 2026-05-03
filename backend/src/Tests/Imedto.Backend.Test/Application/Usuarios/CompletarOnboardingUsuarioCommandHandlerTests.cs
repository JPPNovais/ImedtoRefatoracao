using Imedto.Backend.Application.Usuarios.Commands;
using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Usuarios;

[TestFixture]
public class CompletarOnboardingUsuarioCommandHandlerTests
{
    private Mock<IUsuarioRepository> _repo;
    private CurrentTenantAccessor _tenant;
    private CompletarOnboardingUsuarioCommandHandler _sut;
    private readonly Guid _usuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IUsuarioRepository>();
        // CurrentTenantAccessor é uma classe simples — usar real em vez de mock evita
        // setup repetitivo de UsuarioId/EhDono.
        _tenant = new CurrentTenantAccessor();
        _sut = new CompletarOnboardingUsuarioCommandHandler(_repo.Object, _tenant);
    }

    private static CompletarOnboardingUsuarioCommand Cmd(Guid? usuarioIdNoBody = null) =>
        new()
        {
            UsuarioId = usuarioIdNoBody ?? Guid.NewGuid(), // diferente do JWT — defense-in-depth deve ignorar
            NomeCompleto = "João da Silva",
            Cpf = "123.456.789-09",
            Telefone = "11999998888",
        };

    [Test]
    public async Task Handle_UsuarioJwtValido_CompletaOnboardingEUsaIdDoJwt()
    {
        _tenant.DefinirUsuario(_usuarioId);
        var usuario = Usuario.Criar(_usuarioId, "joao@imedto.com");
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(usuario);
        _repo.Setup(r => r.ExisteCpf("12345678909", _usuarioId)).ReturnsAsync(false);

        // Body propositalmente com OUTRO UsuarioId — deve ser ignorado.
        await _sut.Handle(Cmd(usuarioIdNoBody: Guid.NewGuid()));

        _repo.Verify(r => r.ObterPorIdOuNulo(_usuarioId), Times.Once,
            "Handler deve buscar pelo Id do JWT, ignorando o do body.");
        _repo.Verify(r => r.Salvar(usuario), Times.Once);
        Assert.That(usuario.OnboardingCompleto, Is.True);
        Assert.That(usuario.Status, Is.EqualTo(UsuarioStatus.Ativo));
    }

    [Test]
    public void Handle_UsuarioNaoAutenticado_LancaBusinessException()
    {
        // _tenant.UsuarioId fica Guid.Empty (não chamamos DefinirUsuario).
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não autenticado"));
        _repo.Verify(r => r.Salvar(It.IsAny<Usuario>()), Times.Never);
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

    [Test]
    public void Handle_CpfDuplicado_LancaBusinessException()
    {
        _tenant.DefinirUsuario(_usuarioId);
        var usuario = Usuario.Criar(_usuarioId, "joao@imedto.com");
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(usuario);
        _repo.Setup(r => r.ExisteCpf("12345678909", _usuarioId)).ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("CPF"));
        _repo.Verify(r => r.Salvar(It.IsAny<Usuario>()), Times.Never);
    }

    [Test]
    public async Task Handle_CpfMascaradoNoBody_NormalizaParaConsultaDeDuplicidade()
    {
        _tenant.DefinirUsuario(_usuarioId);
        var usuario = Usuario.Criar(_usuarioId, "joao@imedto.com");
        _repo.Setup(r => r.ObterPorIdOuNulo(_usuarioId)).ReturnsAsync(usuario);
        _repo.Setup(r => r.ExisteCpf(It.IsAny<string>(), _usuarioId)).ReturnsAsync(false);

        await _sut.Handle(Cmd()); // CPF "123.456.789-09"

        _repo.Verify(r => r.ExisteCpf("12345678909", _usuarioId), Times.Once,
            "Handler deve normalizar para somente dígitos antes de consultar duplicidade.");
    }
}
