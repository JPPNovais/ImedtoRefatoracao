using Imedto.Backend.Application.Inventario.Events;
using Imedto.Backend.Domain.Inventario.Events;
using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Inventario;

[TestFixture]
public class EstoqueAbaixoMinimoEventHandlerTests
{
    private Mock<INotificacaoService> _notificacoes;
    private Mock<IInventarioNotificacaoQueryRepository> _destinatariosQuery;
    private EstoqueAbaixoMinimoEventHandler _sut;

    private const long EstabId = 10;
    private const long ItemId = 50;
    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _usuarioComAcaoId = Guid.NewGuid();
    private readonly Guid _usuarioSemAcaoId = Guid.NewGuid(); // apenas para garantir que não aparece

    [SetUp]
    public void SetUp()
    {
        _notificacoes = new Mock<INotificacaoService>();
        _destinatariosQuery = new Mock<IInventarioNotificacaoQueryRepository>();
        _sut = new EstoqueAbaixoMinimoEventHandler(
            _notificacoes.Object,
            _destinatariosQuery.Object,
            NullLogger<EstoqueAbaixoMinimoEventHandler>.Instance);
    }

    private static EstoqueAbaixoMinimoEvent CriarEvento(decimal quantidadeAtual = 7m, decimal quantidadeMinima = 10m)
        => new(ItemId, EstabId, "Seringa", quantidadeAtual, quantidadeMinima);

    /// <summary>
    /// CA6 (parcial): dono + 1 usuário com ação estoque → 2 notificações criadas.
    /// </summary>
    [Test]
    public async Task Handle_DoisDestinatarios_CriaUmaNotificacaoPorUsuario()
    {
        _destinatariosQuery
            .Setup(q => q.ListarUsuariosComAcaoEstoque(EstabId))
            .ReturnsAsync(new[] { _donoId, _usuarioComAcaoId });

        await _sut.Handle(CriarEvento());

        _notificacoes.Verify(
            n => n.EnviarAsync(_donoId, EstabId, It.IsAny<string>(), It.IsAny<string>(),
                               CategoriaNotificacao.Estoque, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Dono deve receber notificação.");

        _notificacoes.Verify(
            n => n.EnviarAsync(_usuarioComAcaoId, EstabId, It.IsAny<string>(), It.IsAny<string>(),
                               CategoriaNotificacao.Estoque, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Usuário com ação estoque deve receber notificação.");

        _notificacoes.Verify(
            n => n.EnviarAsync(It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<string>(), It.IsAny<string>(),
                               It.IsAny<CategoriaNotificacao>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2),
            "Deve criar exatamente 2 notificações.");
    }

    /// <summary>
    /// CA7 (multi-tenant): a query é chamada com o estabelecimentoId do evento — não com outro.
    /// </summary>
    [Test]
    public async Task Handle_SempreFiltraPorEstabelecimentoDoEvento()
    {
        _destinatariosQuery
            .Setup(q => q.ListarUsuariosComAcaoEstoque(EstabId))
            .ReturnsAsync(new[] { _donoId });

        await _sut.Handle(CriarEvento());

        // Garante que a query foi chamada com o estabelecimentoId EXATO do evento.
        _destinatariosQuery.Verify(
            q => q.ListarUsuariosComAcaoEstoque(EstabId),
            Times.Once);

        // Garante que não foi chamada com qualquer outro id (outro tenant).
        _destinatariosQuery.Verify(
            q => q.ListarUsuariosComAcaoEstoque(It.Is<long>(id => id != EstabId)),
            Times.Never,
            "Query de destinatários deve ser filtrada pelo estabelecimento do evento — multi-tenant.");
    }

    /// <summary>
    /// CA7 (notificação criada com estabelecimentoId correto).
    /// </summary>
    [Test]
    public async Task Handle_NotificacaoCriadaComEstabelecimentoIdDoEvento()
    {
        _destinatariosQuery
            .Setup(q => q.ListarUsuariosComAcaoEstoque(EstabId))
            .ReturnsAsync(new[] { _donoId });

        await _sut.Handle(CriarEvento());

        _notificacoes.Verify(
            n => n.EnviarAsync(_donoId, (long?)EstabId, It.IsAny<string>(), It.IsAny<string>(),
                               CategoriaNotificacao.Estoque, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Notificação deve ser criada com o estabelecimentoId do evento.");
    }

    /// <summary>
    /// CA8 (LGPD minimização): a mensagem contém nome do item, quantidades — e NÃO contém
    /// observação de movimentação (o evento não a transporta), CPF, telefone etc.
    /// Verifica que a mensagem menciona o nome do item e as quantidades.
    /// </summary>
    [Test]
    public async Task Handle_MensagemContemNomeEQuantidades_SemPII()
    {
        const string nomeItem = "Seringa";
        var evento = new EstoqueAbaixoMinimoEvent(ItemId, EstabId, nomeItem, 7m, 10m);

        _destinatariosQuery
            .Setup(q => q.ListarUsuariosComAcaoEstoque(EstabId))
            .ReturnsAsync(new[] { _donoId });

        string? mensagemCapturada = null;
        _notificacoes
            .Setup(n => n.EnviarAsync(It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<string>(),
                                      It.IsAny<string>(), It.IsAny<CategoriaNotificacao>(),
                                      It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, long?, string, string, CategoriaNotificacao, string?, CancellationToken>(
                (_, _, _, msg, _, _, _) => mensagemCapturada = msg)
            .Returns(Task.CompletedTask);

        await _sut.Handle(evento);

        Assert.That(mensagemCapturada, Is.Not.Null);
        Assert.That(mensagemCapturada, Does.Contain(nomeItem), "Mensagem deve conter o nome do item.");
        Assert.That(mensagemCapturada, Does.Contain("7"), "Mensagem deve mencionar quantidade atual.");
        Assert.That(mensagemCapturada, Does.Contain("10"), "Mensagem deve mencionar quantidade mínima.");
    }

    /// <summary>
    /// CA15 (resiliência): falha ao notificar um destinatário não deve impedir os demais.
    /// </summary>
    [Test]
    public async Task Handle_FalhaEmUmDestinatario_ContinuaOsOtros()
    {
        _destinatariosQuery
            .Setup(q => q.ListarUsuariosComAcaoEstoque(EstabId))
            .ReturnsAsync(new[] { _donoId, _usuarioComAcaoId });

        // Dono → exceção; usuário com ação → ok.
        _notificacoes
            .Setup(n => n.EnviarAsync(_donoId, It.IsAny<long?>(), It.IsAny<string>(), It.IsAny<string>(),
                                      It.IsAny<CategoriaNotificacao>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Falha simulada de persistência"));

        _notificacoes
            .Setup(n => n.EnviarAsync(_usuarioComAcaoId, It.IsAny<long?>(), It.IsAny<string>(), It.IsAny<string>(),
                                      It.IsAny<CategoriaNotificacao>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Não deve lançar exceção — a falha isolada é absorvida.
        Assert.DoesNotThrowAsync(() => _sut.Handle(CriarEvento()));

        // O segundo destinatário ainda deve ter recebido.
        _notificacoes.Verify(
            n => n.EnviarAsync(_usuarioComAcaoId, It.IsAny<long?>(), It.IsAny<string>(), It.IsAny<string>(),
                               It.IsAny<CategoriaNotificacao>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Segundo destinatário deve receber notificação mesmo com falha no primeiro.");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Sem destinatários com ação estoque: nenhuma notificação criada e não lança.
    /// </summary>
    [Test]
    public async Task Handle_SemDestinatarios_NaoCriaNotificacaoNemLanca()
    {
        _destinatariosQuery
            .Setup(q => q.ListarUsuariosComAcaoEstoque(EstabId))
            .ReturnsAsync(Array.Empty<Guid>());

        Assert.DoesNotThrowAsync(() => _sut.Handle(CriarEvento()));

        _notificacoes.Verify(
            n => n.EnviarAsync(It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<string>(), It.IsAny<string>(),
                               It.IsAny<CategoriaNotificacao>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Categoria da notificação deve ser Estoque — não Sistema ou outra.
    /// </summary>
    [Test]
    public async Task Handle_CategoriaDeveSerEstoque()
    {
        _destinatariosQuery
            .Setup(q => q.ListarUsuariosComAcaoEstoque(EstabId))
            .ReturnsAsync(new[] { _donoId });

        await _sut.Handle(CriarEvento());

        _notificacoes.Verify(
            n => n.EnviarAsync(It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<string>(), It.IsAny<string>(),
                               CategoriaNotificacao.Estoque, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Categoria da notificação deve ser Estoque.");
    }
}
