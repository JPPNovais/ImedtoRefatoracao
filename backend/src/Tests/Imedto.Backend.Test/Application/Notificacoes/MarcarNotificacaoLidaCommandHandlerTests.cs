using Imedto.Backend.Application.Notificacoes.Commands;
using Imedto.Backend.Contracts.Notificacoes.Commands;
using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Notificacoes;

[TestFixture]
public class MarcarNotificacaoLidaCommandHandlerTests
{
    private Mock<INotificacaoRepository> _repo;
    private MarcarNotificacaoLidaCommandHandler _sut;

    private readonly Guid _usuarioId = Guid.NewGuid();
    private readonly Guid _outroUsuarioId = Guid.NewGuid();
    private const long NotificacaoId = 50;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<INotificacaoRepository>();
        _sut = new MarcarNotificacaoLidaCommandHandler(_repo.Object);
    }

    private static Notificacao NotificacaoDoUsuario(Guid uid) =>
        Notificacao.Criar(uid, null, "Titulo", "Mensagem", CategoriaNotificacao.Sistema);

    [Test]
    public async Task Handle_DoProprioUsuario_MarcaComoLida()
    {
        var notif = NotificacaoDoUsuario(_usuarioId);
        _repo.Setup(r => r.ObterPorIdOuNulo(NotificacaoId)).ReturnsAsync(notif);

        await _sut.Handle(new MarcarNotificacaoLidaCommand
        {
            NotificacaoId = NotificacaoId,
            UsuarioId = _usuarioId,
        });

        Assert.That(notif.Lida, Is.True);
        _repo.Verify(r => r.Salvar(notif), Times.Once);
    }

    [Test]
    public void Handle_DeOutroUsuario_LancaMensagemGenericaENaoSalva()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(NotificacaoId)).ReturnsAsync(NotificacaoDoUsuario(_outroUsuarioId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new MarcarNotificacaoLidaCommand
        {
            NotificacaoId = NotificacaoId,
            UsuarioId = _usuarioId,
        }));
        Assert.That(ex.Message, Is.EqualTo("Notificação não encontrada."),
            "Cross-user: mesma mensagem que inexistente — defense-in-depth contra enumeration.");
        _repo.Verify(r => r.Salvar(It.IsAny<Notificacao>()), Times.Never);
    }

    [Test]
    public void Handle_Inexistente_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorIdOuNulo(NotificacaoId)).ReturnsAsync((Notificacao)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new MarcarNotificacaoLidaCommand
        {
            NotificacaoId = NotificacaoId,
            UsuarioId = _usuarioId,
        }));
        Assert.That(ex.Message, Does.Contain("não encontrada"));
    }
}
