using Imedto.Backend.Application.Estabelecimentos.Commands;
using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Common;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Estabelecimentos;

[TestFixture]
public class RemoverFotoEstabelecimentoCommandHandlerTests
{
    private Mock<IEstabelecimentoRepository> _repo;
    private Mock<IFotoStorageService> _storage;
    private Mock<IModeloPermissaoRepository> _permissoes;
    private RemoverFotoEstabelecimentoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _adminComPermissao = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IEstabelecimentoRepository>();
        _storage = new Mock<IFotoStorageService>();
        _permissoes = new Mock<IModeloPermissaoRepository>();
        _sut = new RemoverFotoEstabelecimentoCommandHandler(_repo.Object, _storage.Object, _permissoes.Object);

        _permissoes
            .Setup(p => p.UsuarioTemPermissaoExtra(
                _donoId, EstabelecimentoId, PermissoesExtras.ConfigEstabelecimento,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _permissoes
            .Setup(p => p.UsuarioTemPermissaoExtra(
                _adminComPermissao, EstabelecimentoId, PermissoesExtras.ConfigEstabelecimento,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _permissoes
            .Setup(p => p.UsuarioTemPermissaoExtra(
                _outroId, EstabelecimentoId, PermissoesExtras.ConfigEstabelecimento,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private static Estabelecimento EstabComFoto(Guid donoId, string fotoUrl = "https://cdn.example/estab/1.jpg?sig=abc")
    {
        var estab = Estabelecimento.Criar(donoId, "Clinica", null, null, null, null);
        estab.AlterarFoto(fotoUrl);
        return estab;
    }

    private RemoverFotoEstabelecimentoCommand Cmd(Guid? solicitante = null) => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        UsuarioSolicitanteId = solicitante ?? _donoId,
    };

    [Test]
    public async Task Handle_DonoComFoto_RemoveNoStorageEZeraFotoUrl()
    {
        var estab = EstabComFoto(_donoId, "https://cdn.example/estabelecimentos/1.png?sig=abc");
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);

        await _sut.Handle(Cmd());

        Assert.That(estab.FotoUrl, Is.Null, "Aggregate deve zerar FotoUrl apos remover.");
        _storage.Verify(s => s.RemoverFotoAsync(
            $"estabelecimentos/{EstabelecimentoId}.png",
            It.IsAny<CancellationToken>()), Times.Once,
            "Storage deve receber o path no formato estabelecimentos/{id}.{ext}.");
        _repo.Verify(r => r.Salvar(estab), Times.Once);
    }

    [Test]
    public async Task Handle_AdminComPermissaoConfigEstabelecimento_PodeRemoverFoto()
    {
        // Admin (não-dono) com `config_estabelecimento` também consegue remover.
        var estab = EstabComFoto(_donoId, "https://cdn.example/estabelecimentos/1.png?sig=abc");
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);

        await _sut.Handle(Cmd(solicitante: _adminComPermissao));

        Assert.That(estab.FotoUrl, Is.Null);
        _storage.Verify(s => s.RemoverFotoAsync(
            $"estabelecimentos/{EstabelecimentoId}.png",
            It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.Salvar(estab), Times.Once);
    }

    [Test]
    public async Task Handle_DonoSemFoto_NaoTocaStorageNemPersiste()
    {
        // Estabelecimento sem foto (FotoUrl null) — idempotente, não chama storage nem salva.
        var estab = Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);

        await _sut.Handle(Cmd());

        Assert.That(estab.FotoUrl, Is.Null);
        _storage.Verify(s => s.RemoverFotoAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never,
            "Sem foto previa nao deve invocar o storage (idempotente).");
        _repo.Verify(r => r.Salvar(It.IsAny<Estabelecimento>()), Times.Never,
            "Sem foto previa nao deve persistir (evita ruido em audit).");
    }

    [Test]
    public void Handle_UsuarioSemPermissao_LancaBusinessExceptionGenericaEMantemFoto()
    {
        // Mensagem genérica (LGPD/multi-tenant: não vaza se é por papel ou outro tenant).
        var estab = EstabComFoto(_donoId);
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroId)));
        Assert.That(ex.Message, Does.Contain("permissão"));
        Assert.That(estab.FotoUrl, Is.Not.Null, "FotoUrl deve permanecer intacta quando autorizacao falha.");
        _storage.Verify(s => s.RemoverFotoAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never,
            "Storage nao deve ser tocado quando autorizacao falha — desperdicio + risco.");
    }

    [Test]
    public void Handle_EstabelecimentoInexistente_LancaBusinessExceptionGenerica()
    {
        // Mensagem generica "nao encontrado" — protecao multi-tenant: nao revela
        // se o id existe em outro tenant.
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync((Estabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
        _storage.Verify(s => s.RemoverFotoAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_FotoUrlComQueryStringPresigned_ExtraiExtensaoCorreta()
    {
        // Presigned URL do S3 vem com ?X-Amz-Signature=... — o handler precisa
        // extrair a extensao do path, ignorando a query string.
        var estab = EstabComFoto(_donoId, "https://bucket.s3.amazonaws.com/estabelecimentos/1.webp?X-Amz-Signature=abc&Expires=999");
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);

        await _sut.Handle(Cmd());

        _storage.Verify(s => s.RemoverFotoAsync(
            $"estabelecimentos/{EstabelecimentoId}.webp",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_FotoUrlSemExtensao_FazFallbackParaJpg()
    {
        // URL malformada / sem extensao no path → handler usa "jpg" como default
        // (coerente com o upload, que tambem cai em jpg quando nao tem extensao).
        var estab = EstabComFoto(_donoId, "https://cdn.example/foto");
        _repo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(estab);

        await _sut.Handle(Cmd());

        _storage.Verify(s => s.RemoverFotoAsync(
            $"estabelecimentos/{EstabelecimentoId}.jpg",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
