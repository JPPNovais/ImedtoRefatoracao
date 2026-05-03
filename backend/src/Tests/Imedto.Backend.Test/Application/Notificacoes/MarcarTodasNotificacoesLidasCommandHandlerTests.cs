using Imedto.Backend.Application.Notificacoes.Commands;
using Imedto.Backend.Contracts.Notificacoes.Commands;
using Imedto.Backend.Domain.Notificacoes;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Notificacoes;

[TestFixture]
public class MarcarTodasNotificacoesLidasCommandHandlerTests
{
    private Mock<INotificacaoRepository> _repo;
    private MarcarTodasNotificacoesLidasCommandHandler _sut;
    private readonly Guid _usuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<INotificacaoRepository>();
        _sut = new MarcarTodasNotificacoesLidasCommandHandler(_repo.Object);
    }

    [Test]
    public async Task Handle_DelegaParaRepoComUsuarioIdDoCommand()
    {
        _repo.Setup(r => r.MarcarTodasLidasDoUsuario(_usuarioId)).ReturnsAsync(7);

        await _sut.Handle(new MarcarTodasNotificacoesLidasCommand { UsuarioId = _usuarioId });

        _repo.Verify(r => r.MarcarTodasLidasDoUsuario(_usuarioId), Times.Once);
        _repo.VerifyNoOtherCalls();
    }
}
