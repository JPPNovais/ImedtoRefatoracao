using Imedto.Backend.Application.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Vinculos;

/// <summary>
/// Bug #4 — aceite automatico de convites pendentes no fluxo
/// <c>POST /api/auth/aceitar-convite</c>. Confirma que o handler:
///   - Aceita TODOS os pendentes do usuario (cenario tipico = 1).
///   - Publica <c>VinculoAceitoEvent</c> para cada vinculo aceito.
///   - É idempotente: nenhum pendente = no-op silencioso.
///   - Recusa <c>Guid.Empty</c> com 422 (defesa basica de input).
/// </summary>
[TestFixture]
public class AceitarConvitesPendentesDoUsuarioCommandHandlerTests
{
    private Mock<IVinculoRepository> _repo;
    private Mock<IEventBus> _eventBus;
    private AceitarConvitesPendentesDoUsuarioCommandHandler _sut;

    private readonly Guid _usuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IVinculoRepository>();
        _eventBus = new Mock<IEventBus>();
        _sut = new AceitarConvitesPendentesDoUsuarioCommandHandler(_repo.Object, _eventBus.Object);
    }

    private VinculoProfissionalEstabelecimento CriarConvite(long estabelecimentoId) =>
        VinculoProfissionalEstabelecimento.Convidar(
            profissionalUsuarioId: _usuarioId,
            estabelecimentoId: estabelecimentoId,
            modeloPermissaoId: 10L,
            convidadoPorUsuarioId: Guid.NewGuid());

    [Test]
    public async Task Handle_UsuarioComUmConvitePendente_AceitaEPublicaEvento()
    {
        var convite = CriarConvite(1);
        _repo.Setup(r => r.ListarPendentesPorUsuario(_usuarioId))
            .ReturnsAsync(new[] { convite });

        await _sut.Handle(new AceitarConvitesPendentesDoUsuarioCommand
        {
            ProfissionalUsuarioId = _usuarioId
        });

        Assert.That(convite.Status, Is.EqualTo(VinculoStatus.Ativo));
        Assert.That(convite.AceitoEm, Is.Not.Null);
        _repo.Verify(r => r.Salvar(convite), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is VinculoAceitoEvent)), Times.Once);
    }

    [Test]
    public async Task Handle_UsuarioComMultiplosPendentes_AceitaTodos()
    {
        // Cenario menos comum mas valido: usuario convidado para 2 clinicas.
        // A correcao precisa aceitar TODOS no mesmo passo — caso contrario o
        // usuario ainda teria de abrir /meus-convites para os restantes.
        var c1 = CriarConvite(1);
        var c2 = CriarConvite(2);
        _repo.Setup(r => r.ListarPendentesPorUsuario(_usuarioId))
            .ReturnsAsync(new[] { c1, c2 });

        await _sut.Handle(new AceitarConvitesPendentesDoUsuarioCommand
        {
            ProfissionalUsuarioId = _usuarioId
        });

        Assert.That(c1.Status, Is.EqualTo(VinculoStatus.Ativo));
        Assert.That(c2.Status, Is.EqualTo(VinculoStatus.Ativo));
        _repo.Verify(r => r.Salvar(It.IsAny<VinculoProfissionalEstabelecimento>()), Times.Exactly(2));
        _eventBus.Verify(b => b.Publish(It.IsAny<IDomainEvent>()), Times.Exactly(2));
    }

    [Test]
    public async Task Handle_UsuarioSemPendentes_NaoFazNada()
    {
        _repo.Setup(r => r.ListarPendentesPorUsuario(_usuarioId))
            .ReturnsAsync(Array.Empty<VinculoProfissionalEstabelecimento>());

        await _sut.Handle(new AceitarConvitesPendentesDoUsuarioCommand
        {
            ProfissionalUsuarioId = _usuarioId
        });

        _repo.Verify(r => r.Salvar(It.IsAny<VinculoProfissionalEstabelecimento>()), Times.Never);
        _eventBus.Verify(b => b.Publish(It.IsAny<IDomainEvent>()), Times.Never);
    }

    [Test]
    public void Handle_UsuarioVazio_LancaBusinessException()
    {
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new AceitarConvitesPendentesDoUsuarioCommand
        {
            ProfissionalUsuarioId = Guid.Empty
        }));
        _repo.Verify(r => r.ListarPendentesPorUsuario(It.IsAny<Guid>()), Times.Never);
    }
}
